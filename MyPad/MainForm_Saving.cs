﻿using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

using MyPad.Dialogs;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

//using LuaInterface;

namespace MyPad
{
    public partial class MainForm : Form
    {
        UnsavedDocumentsDialog unsavedDocumentsDialog;

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            saveToolStripMenuItem_Click(null, null);
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            saveAllToolStripMenuItem_Click(null, null);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditorTabPage etb = GetActiveTab();
            if (etb != null)
            {
                SaveTabToDisk(etb); 
            }
        }
        /// <summary>
        /// Вызывается из главного меню, когда надо сохранить файл
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditorTabPage etb = GetActiveTab();

            if (etb != null)
            {
                string newName = etb.Editor.ActiveTextAreaControl.SelectionManager.SelectedText;
                if (string.IsNullOrEmpty(newName))
                {
                    newName = "index.htm";
                }
                FileInfo finfo = new FileInfo(etb.ToolTipText);
                string proposedFileName = Path.Combine(finfo.DirectoryName, newName);
                CreateNewTabWithProposedFileName(proposedFileName);
                // GetNewNameInSaveAsDialogFromProposedName(proposedFileName);
            }
        }

        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditorTabPage currentActiveTab = GetActiveTab();

            foreach (EditorTabPage etb in tabControl1.TabPages)
            {
                tabControl1.SelectedTab = etb;
                SaveTabToDisk(etb); 
            }

            tabControl1.SelectedTab = currentActiveTab;
        }


        string GetNewNameInSaveAsDialogFromProposedName(string proposedFileName)
        {
            saveFileDialog1.FileName = proposedFileName;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveFileDialog1.FileName;
                return fileName;
            }
            return null;
        }

        string GetRelativeUriString (string oldTabFullName, string newTabFullName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace (oldTabFullName) == false)
                {
                    if (string.IsNullOrWhiteSpace (newTabFullName) == false)
                    {
                        Uri fromUri = new Uri (oldTabFullName);
                        Uri toUri = new Uri (newTabFullName);

                        if (fromUri.Scheme == toUri.Scheme) // path can't be made relative.
                        {

                            Uri relativeUri = fromUri.MakeRelativeUri (toUri);
                            String relativePath = Uri.UnescapeDataString (relativeUri.ToString ());
                            return relativePath;
                        }
                    }
                }
            }
            catch
            {
            }
            return String.Empty;
        }

        /// <summary>
        /// Создаёт новую вкладку, размещает в ней текст
        /// </summary>
        /// <param name="fileName">File name.</param>
        private EditorTabPage  CreateNewTabWithProposedFileName(string fileName)
        {
            string newTabName = fileName;
            EditorTabPage currentActiveTab = GetActiveTab();

            EditorTabPage etb = new EditorTabPage();
            etb.SetFileName(newTabName);

            var relUri = GetRelativeUriString (currentActiveTab.ToolTipText, newTabName);
            etb.Editor.Text = GetDefaultTemplateText(fileName, relUri);

            etb.Editor.DragEnter += new DragEventHandler(tabControl1_DragEnter);
            etb.Editor.DragDrop += new DragEventHandler(tabControl1_DragDrop);
            etb.OnEditorTextChanged += new EventHandler(etb_TextChanged);
            etb.OnEditorTabFilenameChanged += new EventHandler(TabControl_TabCaptionUpdate);
            etb.OnEditorTabStateChanged += new EventHandler(TabControl_TabCaptionUpdate);
            etb.Show();

            tabControl1.TabPages.Add(etb);
            tabControl1.SelectedTab = etb;

            return etb;
        }

        void SaveTabToDisk(EditorTabPage etb)
        {
            string fileName = etb.ToolTipText;
            // tooltip is exact name of file, and Text property of tab may contain "*" if file is modified and unsaved
            FileInfo fi = new FileInfo(fileName);
            if (fi.Name.StartsWith("Untitled"))
            {
                // propose to reneame file
                var newPageName = GetNewNameInSaveAsDialogFromProposedName(fi.FullName);
                if (string.IsNullOrWhiteSpace(newPageName))
                {
                    return;
                    // user stopped save operation
                }
                //var etb = DoRealSaveAs(newPageName); there is no need for new tab in simple save operation
                fileName = newPageName;
            }
            else
            {
                // create deirectory if necessary
                if (Directory.Exists(fi.DirectoryName) == false)
                {
                    // Message with alert?
                    fi.Directory.Create();
                }
            }
            etb.SaveFile(fileName);
            AppendToMRU(fileName);
        }

        private void AppendToMRU(string filename)
        {
            if (!SettingsManager.MRUList.Contains(filename))
            {
                if (SettingsManager.MRUList.Count >= 15)
                    SettingsManager.MRUList.RemoveAt(14);
                SettingsManager.MRUList.Insert(0, filename);

                ToolStripMenuItem tsi = new ToolStripMenuItem(filename, null, new EventHandler(RecentFiles_Click));
                recentFilesToolStripMenuItem.DropDown.Items.Insert(0, tsi);
            }
            else
            {
                SettingsManager.MRUList.Remove(filename);
                SettingsManager.MRUList.Insert(0, filename);

                ToolStripMenuItem tsi = GetRecentMenuItem(filename);
                recentFilesToolStripMenuItem.DropDown.Items.Remove(tsi);
                recentFilesToolStripMenuItem.DropDown.Items.Insert(0, tsi);
            }
        }
    }
}

