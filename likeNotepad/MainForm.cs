using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

public class Program
{
    [STAThread]
    public static void Main()
    {
        new Application(Eto.Platform.Detect).Run(new MainForm());
    }
}

public class MainForm : Form
{
    TextArea textArea;

	Label statusLabel;

	Command cmdNewFile;
	Command cmdNewWin;
    Command cmdOpen;
    Command cmdSave;
	Command cmdSaveAs;
	CheckMenuItem checkWordWrap;
	Command cmdFont;
	Command cmdZoomIn;
	Command cmdZoomOut;
	Command cmdRestoreZoom;
	Command cmdSendFeedback;

	string filePath = "";
	string baseFile = "Untitled";

	float originalFontSize = 11;
	float zoomFactor = 1.00f;

	FontStyle textStyle = FontStyle.None;
	string textFont;

	bool useCRLF = false; // By default, use LF.

    public MainForm()
    {
        string[] args = Environment.GetCommandLineArgs();
		if (args.Length == 2) {
			if (File.Exists(args[1])) {
				filePath = args[1];
				baseFile = Path.GetFileName(args[1]);
			}
		}

		Closing += MainForm_Closing;

        Title = baseFile + " - likeNotepad";

		SizeF screenSize;
		try {
			screenSize = Screen.PrimaryScreen.Bounds.Size;
		} catch {
			screenSize = new SizeF(1920, 1080);
		}
		Size = new Size((int)(screenSize.Width * 0.283), (int)(screenSize.Height * 0.273));

        textArea = new TextArea();
		textFont = PreferredFont();
		if (textFont != "") {
			textArea.Font = new Font(textFont,originalFontSize*zoomFactor,textStyle);
		}

		if (filePath != "") {
			textArea.Text = File.ReadAllText(filePath);

			// Detect line endings and set the flag
			var lineEndingType = CheckAndFixLineEndings(textArea.Text);
			if (lineEndingType == "Win32/DOS (CRLF)")
				useCRLF = true;
			else
				useCRLF = false;
		}

		textArea.TextChanged += textArea_TextChanged;
		textArea.CaretIndexChanged += textArea_CaretIndexChanged;

		cmdNewFile = new Command { MenuText = "New", Shortcut = Application.Instance.CommonModifier | Keys.N };
		cmdNewFile.Executed += cmdNewFile_Executed;

		cmdNewWin = new Command { MenuText = "New Window", Shortcut = Application.Instance.CommonModifier | Keys.Shift | Keys.N };
		cmdNewWin.Executed += cmdNewWin_Executed;

        cmdOpen = new Command { MenuText = "Open...", Shortcut = Application.Instance.CommonModifier | Keys.O };
        cmdOpen.Executed += cmdOpen_Executed;

        cmdSave = new Command { MenuText = "Save", Shortcut = Application.Instance.CommonModifier | Keys.S };
        cmdSave.Executed += cmdSave_Executed;

		cmdSaveAs = new Command { MenuText = "Save As...", Shortcut = Application.Instance.CommonModifier | Keys.Shift | Keys.S };
        cmdSaveAs.Executed += cmdSaveAs_Executed;

		checkWordWrap = new CheckMenuItem { Text = "Word Wrap" };
		checkWordWrap.CheckedChanged += checkWordWrap_CheckedChanged;
		checkWordWrap.Checked = false;
		textArea.Wrap = checkWordWrap.Checked;

		cmdFont = new Command { MenuText = "Font..." };
		cmdFont.Executed += cmdFont_Executed;

		cmdZoomIn = new Command { MenuText = "Zoom In", Shortcut = Application.Instance.CommonModifier | Keys.Equal };
		cmdZoomIn.Executed += cmdZoomIn_Executed;

		cmdZoomOut = new Command { MenuText = "Zoom Out", Shortcut = Application.Instance.CommonModifier | Keys.Minus };
		cmdZoomOut.Executed += cmdZoomOut_Executed;

		cmdRestoreZoom = new Command { MenuText = "Restore Zoom", Shortcut = Application.Instance.CommonModifier | Keys.D0 };
		cmdRestoreZoom.Executed += cmdRestoreZoom_Executed;

		cmdSendFeedback = new Command { MenuText = "Send Feedback" };
		cmdSendFeedback.Executed += (sender, e) => Process.Start("/usr/bin/sh", "/usr/bin/open https://github.com/z-izz/likeNotepad/issues");

        MenuBar menu = new MenuBar
		{
			Items =
			{
				new ButtonMenuItem { Text = "&File", Items = { cmdNewFile, cmdNewWin, cmdOpen, cmdSave, cmdSaveAs } },
				new ButtonMenuItem { Text = "&Format", Items = { checkWordWrap, cmdFont } },
				new ButtonMenuItem { Text = "&View", Items = { cmdZoomIn, cmdZoomOut, cmdRestoreZoom} },
				new ButtonMenuItem { Text = "&Help", Items = { cmdSendFeedback } }
			},
			QuitItem = new ButtonMenuItem { Text = "&Exit", Command = new Command((sender, e) => { Close(); }) },
			AboutItem = new ButtonMenuItem { Text = "&About likeNotepad", Command = new Command((sender, e) => { cmdAbout_Executed(sender, e); }) }
		};

		
		// Custom status bar using Panel
		var pos = GetCaretPosition(textArea);
        statusLabel = new Label { Text = $"Ln {pos.Line}, Col {pos.Column} | {Math.Round(zoomFactor * 100)}% | {CheckAndFixLineEndings(textArea.Text)}"};
		Color statusBackgroundColor = textArea.BackgroundColor;
		statusBackgroundColor.R -= 0.2f;
		statusBackgroundColor.G -= 0.2f;
		statusBackgroundColor.B -= 0.2f;
        Panel statusBar = new Panel 
        { 
            Padding = new Padding(5), 
            Content = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Stretch, // Stretch to ensure full width
                Items =
                {
                    new StackLayoutItem { Expand = true }, // Empty item to push the label to the right
                    new StackLayoutItem(statusLabel, HorizontalAlignment.Right)
                }
            },
            BackgroundColor = statusBackgroundColor // Optional, if you want a distinct background
        };

        Menu = menu;

		Content = new TableLayout
        {
            Rows =
            {
                new TableRow(textArea) { ScaleHeight = true }, // This ensures TextArea takes up most of the space
                statusBar 
            }
        };
    }

	void UpdateStatus()
    {
		var pos = GetCaretPosition(textArea);
        statusLabel.Text = $"Ln {pos.Line}, Col {pos.Column} | {Math.Round(zoomFactor * 100)}% | {CheckAndFixLineEndings(textArea.Text)}";
    }

	public string CheckAndFixLineEndings(string content)
	{
		bool hasCRLF = false;
		bool hasLF = false;

		char prevChar = '\0';
		foreach (char currentChar in content)
		{
			if (prevChar == '\r' && currentChar == '\n')
			{
				hasCRLF = true;
			}
			else if (currentChar == '\n')
			{
				hasLF = true;
			}

			// If both types of line endings are detected, handle them:
			if (hasCRLF && hasLF)
			{
				// First, standardize all to LF:
				content = content.Replace("\r\n", "\n");

				// If originally CRLF, convert them all back to CRLF:
				if (useCRLF)
				{
					content = content.Replace("\n", "\r\n");
					textArea.Text = content; // Update the TextArea with the adjusted content.
				}
			}

			prevChar = currentChar;
		}

		if (hasCRLF)
		{
			useCRLF = true; // set the flag for further edits
			return "Win32/DOS (CRLF)";
		}
		else if (hasLF)
		{
			useCRLF = false; // set the flag for further edits
			return "Unix (LF)";
		}

		return "Unknown";
	}

	(int Line, int Column) GetCaretPosition(TextArea textArea)
	{
		int index = textArea.CaretIndex;
		int line = 1;
		int column = 1;

		for (int i = 0; i < index; i++)
		{
			if (textArea.Text[i] == '\n')
			{
				line++;
				column = 1;
			}
			else
			{
				column++;
			}
		}

		return (Line: line, Column: column);
	}

	string EnsureCorrectLineEndings(string content, bool useCRLF)
	{
		if (useCRLF)
		{
			// First, make sure all line endings are in LF format.
			content = content.Replace("\r\n", "\n");
			// Convert LF to CRLF.
			content = content.Replace("\n", "\r\n");
		}
		return content;
	}

	void MainForm_Closing(object sender, CancelEventArgs e)
	{
		bool Saved = true;
		if (baseFile.StartsWith("*")) {
			if (Title.StartsWith("**")) {
				Saved = false;
			}
		}
		else {
			if (Title.StartsWith("*")) {
				Saved = false;
			}
		}
		if (!Saved) {
			// Prompt the user for confirmation
			var msgResult = MessageBox.Show(this, "Do you want to save changes to " + baseFile + "?", "likeNotepad", MessageBoxButtons.YesNoCancel, MessageBoxType.Question, MessageBoxDefaultButton.Yes);
			
			if (msgResult == DialogResult.Cancel)
			{
				e.Cancel = true; // Cancel the closing process
			}
			else if (msgResult == DialogResult.Yes) {
				string contentToSave = EnsureCorrectLineEndings(textArea.Text, useCRLF);

				if (filePath == "")
				{
					var sfd = new SaveFileDialog();
					var result = sfd.ShowDialog(this);
					if (result == DialogResult.Ok)
					{
						filePath = sfd.FileName;
						baseFile = Path.GetFileName(sfd.FileName);
						File.WriteAllText(sfd.FileName, contentToSave);
						Title = baseFile + " - likeNotepad";
					}
				}
				else
				{
					File.WriteAllText(filePath, contentToSave);
					Title = baseFile + " - likeNotepad";
				}
			}
		}
	}

	void textArea_TextChanged(object sender, EventArgs e) {
		Title = "*" + baseFile + " - likeNotepad";
	}

	void textArea_CaretIndexChanged(object sender, EventArgs e) {
		UpdateStatus();
	}

	void cmdNewFile_Executed(object sender, EventArgs e) {
		bool Saved = true;
		if (baseFile.StartsWith("*")) {
			if (Title.StartsWith("**")) {
				Saved = false;
			}
		}
		else {
			if (Title.StartsWith("*")) {
				Saved = false;
			}
		}
		if (!Saved) {
			// Prompt the user for confirmation
			var msgResult = MessageBox.Show(this, "Do you want to save changes to " + baseFile + "?", "likeNotepad", MessageBoxButtons.YesNoCancel, MessageBoxType.Question, MessageBoxDefaultButton.Yes);
			
			if (msgResult == DialogResult.Cancel)
			{
				return;
			}
			else if (msgResult == DialogResult.Yes) {
				string contentToSave = EnsureCorrectLineEndings(textArea.Text, useCRLF);

				if (filePath == "")
				{
					var sfd = new SaveFileDialog();
					var result = sfd.ShowDialog(this);
					if (result == DialogResult.Ok)
					{
						filePath = sfd.FileName;
						baseFile = Path.GetFileName(sfd.FileName);
						File.WriteAllText(sfd.FileName, contentToSave);
					}
				}
				else
				{
					File.WriteAllText(filePath, contentToSave);
				}
			}
		}

		filePath = "";
		baseFile = "Untitled";
		textArea.Text = "";
		Title = baseFile + " - likeNotepad";
	}

	void cmdNewWin_Executed(object sender, EventArgs e) {
		var newWindow = new MainForm();
    	newWindow.Show();
	}

	void cmdOpen_Executed(object sender, EventArgs e)
	{
		var ofd = new OpenFileDialog();
		var result = ofd.ShowDialog(this);
		if (result == DialogResult.Ok)
		{
			textArea.Text = File.ReadAllText(ofd.FileName);
			filePath = ofd.FileName;
			baseFile = Path.GetFileName(ofd.FileName);
			Title = baseFile + " - likeNotepad";

			// Detect line endings and set the flag
			var lineEndingType = CheckAndFixLineEndings(textArea.Text);
			if (lineEndingType == "Win32/DOS (CRLF)")
				useCRLF = true;
			else
				useCRLF = false;
		}
	}

    void cmdSave_Executed(object sender, EventArgs e)
	{
		string contentToSave = EnsureCorrectLineEndings(textArea.Text, useCRLF);

		if (filePath == "")
		{
			var sfd = new SaveFileDialog();
			var result = sfd.ShowDialog(this);
			if (result == DialogResult.Ok)
			{
				filePath = sfd.FileName;
				baseFile = Path.GetFileName(sfd.FileName);
				File.WriteAllText(sfd.FileName, contentToSave);
				Title = baseFile + " - likeNotepad";
			}
		}
		else
		{
			File.WriteAllText(filePath, contentToSave);
			Title = baseFile + " - likeNotepad";
		}
	}

	void cmdSaveAs_Executed(object sender, EventArgs e)
	{
		string contentToSave = EnsureCorrectLineEndings(textArea.Text, useCRLF);

		var sfd = new SaveFileDialog();
		var result = sfd.ShowDialog(this);
		if (result == DialogResult.Ok)
		{
			filePath = sfd.FileName;
			baseFile = Path.GetFileName(sfd.FileName);
			File.WriteAllText(sfd.FileName, contentToSave);
			Title = baseFile + " - likeNotepad";
		}
	}

	void checkWordWrap_CheckedChanged(object sender, EventArgs e) {
		textArea.Wrap = checkWordWrap.Checked;
	}

	void cmdFont_Executed(object sender, EventArgs e) {
		FontDialog fontDialog = new FontDialog();
		fontDialog.Font = textArea.Font;

		var result = fontDialog.ShowDialog(this);
		if (result == DialogResult.Ok) {
			textFont = fontDialog.Font.FamilyName;
			textStyle = fontDialog.Font.FontStyle;
			originalFontSize = fontDialog.Font.Size;
			textArea.Font = new Font(textFont,originalFontSize*zoomFactor,textStyle);
		}
	}

	void cmdZoomIn_Executed(object sender, EventArgs e) {
		if (zoomFactor < 49.9f) {
			zoomFactor += 0.10f;
		}
		textArea.Font = new Font(textFont,originalFontSize*zoomFactor,textStyle);
		UpdateStatus();
	}

	void cmdZoomOut_Executed(object sender, EventArgs e) {
		if (zoomFactor > 0.10f) {
			zoomFactor -= 0.10f;
		}
		textArea.Font = new Font(textFont,originalFontSize*zoomFactor,textStyle);
		UpdateStatus();
	}

	void cmdRestoreZoom_Executed(object sender, EventArgs e) {
		zoomFactor = 1.00f;
		textArea.Font = new Font(textFont,originalFontSize*zoomFactor,textStyle);
		UpdateStatus();
	}

	void cmdAbout_Executed(object sender, EventArgs e)
	{
		var aboutDialog = new Dialog
		{
			Title = "About likeNotepad",
			ClientSize = new Size(500, 245)
		};

		var aboutTextArea = new TextArea
		{
			ReadOnly = true,
			TextAlignment = TextAlignment.Center,
			Cursor = Cursors.Default,
			Enabled = false,
			Text = @"
likeNotepad

A simple text editor that is similar to Win32 Notepad, but for Linux.

Licensed under GNU General Public License (GPL) version 2.0.

Created with Eto.Forms.

Copyleft (c) 2023 z-izz and contributors."
		};

		var closeButton = new Button { Text = "Close" };
		var githubButton = new Button { Text = "GitHub" };
		closeButton.Click += (s, ev) => aboutDialog.Close();
		githubButton.Click += (s, ev) => Process.Start("/usr/bin/sh", "/usr/bin/open https://github.com/z-izz/likeNotepad");
		
		aboutDialog.PositiveButtons.Add(closeButton);
		aboutDialog.NegativeButtons.Add(githubButton);

		aboutDialog.Content = aboutTextArea;

		aboutDialog.ShowModal();
	}

	string PreferredFont()
	{
		var preferredFonts = new List<string> { "Consolas", "Cascadia Code", "Ubuntu Mono", "Monospace", "Courier New" };

		foreach (var fontName in preferredFonts)
		{
			if (FontAvailable(fontName))
			{
				return fontName;
			}
		}

		return "";
	}

	bool FontAvailable(string fontName)
	{
		try {
			new Font(fontName, 1);
			return true;
		}
		catch {
			return false;
		}
	}
}
