﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.CSharp;
using Microsoft.Win32.TaskScheduler;
using System.ComponentModel;

namespace TestTaskService
{
	internal partial class ScriptTestDlg : Form
	{
		private static readonly string[] validExts = { ".cs", ".txt" };

		private CompilerParameters compilerParams;
		private string lastFileName;
		private CSharpCodeProvider provider;

		public ScriptTestDlg()
		{
			InitializeComponent();
			codeEditor.AllowDrop = true;
			codeEditor.DragDrop += CodeEditor_DragDrop;
			codeEditor.DragEnter += CodeEditor_DragEnter;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TaskService TaskService { get; set; }

		private void closeBtn_Click(object sender, EventArgs e)
		{
			Properties.Settings.Default.Save();
			Close();
		}

		private void CodeEditor_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.Text, true))
				codeEditor.SelectedText = e.Data.GetData(DataFormats.Text, true).ToString();
			else
			{
				if (IsValidDropFile(e) != null)
					codeEditor.Text = File.ReadAllText(IsValidDropFile(e));
			}
		}

		private void CodeEditor_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.Text, true))
				e.Effect = DragDropEffects.Copy;
			else if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = (IsValidDropFile(e) != null) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			codeEditor.Copy();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			codeEditor.Cut();
		}

		private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			var hasText = codeEditor.TextLength > 0;
			saveToolStripMenuItem.Enabled = hasText && !string.IsNullOrEmpty(lastFileName);
			saveAsToolStripMenuItem.Enabled = printToolStripMenuItem.Enabled = hasText;
			undoToolStripMenuItem.Enabled = codeEditor.CanUndo;
			redoToolStripMenuItem.Enabled = codeEditor.CanRedo;
			cutToolStripMenuItem.Enabled = copyToolStripMenuItem.Enabled = codeEditor.SelectionLength > 0;
			pasteToolStripMenuItem.Enabled = codeEditor.CanPaste(DataFormats.GetFormat(DataFormats.Text));
		}

		private string IsValidDropFile(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				foreach (var f in (string[])e.Data.GetData(DataFormats.FileDrop))
				{
					var ext = Path.GetExtension(f);
					foreach (var s in validExts)
						if (string.Compare(s, ext, true) == 0)
							return f;
				}
			}
			return null;
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			codeEditor.Clear();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
				codeEditor.Text = File.ReadAllText(lastFileName = openFileDialog1.FileName);
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			codeEditor.Paste(DataFormats.GetFormat(DataFormats.Text));
		}

		private void printToolStripMenuItem_Click(object sender, EventArgs e)
		{
		}

		private void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			codeEditor.Redo();
		}

		private void runBtn_Click(object sender, EventArgs e)
		{
			if (provider == null)
			{
				var providerOptions = new Dictionary<string, string>(1) { { "CompilerVersion", "v4.0" } };
				provider = new CSharpCodeProvider(providerOptions);
				var thisDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var asms = new[] { "System.dll", "System.Xml.dll", "System.Core.dll", "System.Windows.Forms.dll", "System.Drawing.dll", Path.Combine(thisDir, "Microsoft.Win32.TaskScheduler.dll"), Path.Combine(thisDir, "Microsoft.Win32.TaskSchedulerEditor.dll") };
				compilerParams = new CompilerParameters(asms) { GenerateInMemory = true, GenerateExecutable = false, OutputAssembly = "RuntimeAsm" };
			}

			var code = string.Concat(@"using System; using System.Collections.Generic; using System.Linq; using System.Windows.Forms; using Microsoft.Win32.TaskScheduler; namespace RuntimeNS { public static class RuntimeRunner { public static void Run(TaskService ts, System.IO.StringWriter output) { ", codeEditor.Text, @" } } }");
			var results = provider.CompileAssemblyFromSource(compilerParams, code);
			if (results.Errors.Count != 0)
			{
				var strArr = new string[results.Output.Count];
				results.Output.CopyTo(strArr, 0);
				ShowSidePanel(string.Join("\r\n", strArr), "Compiler Errors");
			}
			else
			{
				var mi = results.CompiledAssembly.GetType("RuntimeNS.RuntimeRunner").GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
				var sw = new StringWriter();
				try { mi?.Invoke(null, new object[] { TaskService, sw }); ShowSidePanel(sw.ToString(), "Results"); }
				catch (Exception ex) { ShowSidePanel(ex.ToString(), "Exception"); }
			}
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
				File.WriteAllText(saveFileDialog.FileName, codeEditor.Text);
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(lastFileName))
				File.WriteAllText(lastFileName, codeEditor.Text);
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			codeEditor.SelectAll();
		}

		private void ShowSidePanel(string content, string heading)
		{
			textBox2.Text = content;
			headingLabel.Text = heading;
			splitContainer1.Panel2Collapsed = false;
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			codeEditor.Undo();
		}
	}
}