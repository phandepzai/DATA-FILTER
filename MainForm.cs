using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DATAFILTER
{
    public partial class MainForm : Form
    {
        #region KHAI BÁO CÁC BIẾN
        private readonly Color evenRowColor = Color.White;
        private readonly Color oddRowColor = Color.FromArgb(220, 220, 220);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;
        private readonly BackgroundWorker filterWorker;
        private int lastHighlightedLineInput = -1;
        private int lastHighlightedLineResult = -1;
        private const int LARGE_DATA_THRESHOLD = 5000;
        private List<int> lastHighlightedLinesInput = new List<int>();
        private List<int> lastHighlightedLinesResult = new List<int>();
        private bool isInputPlaceholder = true;
        private bool isResultPlaceholder = true;
        // ✅ THÊM: Biến để tắt tạm thời TextChanged event
        private bool suppressTextChangedEvent = false;
        #endregion

        #region PHẦN KHƠI TẠO CHÍNH
        public MainForm()
        {
            InitializeComponent();
            inputTextBox.TextChanged += InputTextBox_TextChanged;
            resultTextBox.TextChanged += ResultTextBox_TextChanged;

            inputTextBox.WordWrap = false;
            resultTextBox.WordWrap = false;
            inputTextBox.ScrollBars = RichTextBoxScrollBars.Both;
            resultTextBox.ScrollBars = RichTextBoxScrollBars.Both;

            inputTextBox.BorderStyle = BorderStyle.None;
            resultTextBox.BorderStyle = BorderStyle.None;

            inputTextBox.ContextMenuStrip = null;
            resultTextBox.ContextMenuStrip = null;

            SetPlaceholder(inputTextBox, "Paste dữ liệu vào đây...");
            SetPlaceholder(resultTextBox, "Kết quả sẽ hiển thị ở đây...");

            inputTextBox.KeyDown += InputTextBox_KeyDown;

            CreateContextMenus();

            UpdateLineCount();
            lineCountComboBox.SelectedIndex = 0;

            filterWorker = new BackgroundWorker();
            filterWorker.DoWork += FilterWorker_DoWork;
            filterWorker.RunWorkerCompleted += FilterWorker_RunWorkerCompleted;
            filterWorker.WorkerSupportsCancellation = true;

            inputTextBox.MouseClick += InputTextBox_MouseClick;
            resultTextBox.MouseClick += ResultTextBox_MouseClick;
        }
        #endregion

        #region TẠO MENU CHUỘT PHẢI
        private void CreateContextMenus()
        {
            ContextMenuStrip inputMenu = new ContextMenuStrip();

            ToolStripMenuItem pasteItem = new ToolStripMenuItem("Dán từ cliboard", null, (s, e) =>
            {
                try
                {
                    string pastedText = Clipboard.GetText();
                    pastedText = pastedText.Replace("\r\n", "\n");
                    pastedText = pastedText.Replace("\r", "\n");
                    pastedText = pastedText.Trim();

                    SetTextWithIndent(inputTextBox, pastedText);
                    inputTextBox.ForeColor = Color.Black;
                    isInputPlaceholder = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi paste: {ex.Message}");
                }
            });

            ToolStripMenuItem clearItem = new ToolStripMenuItem("Làm sạch", null, (s, e) =>
            {
                inputTextBox.Clear();
                SetPlaceholder(inputTextBox, "Paste dữ liệu vào đây...");
                isInputPlaceholder = true;
                UpdateLineCount();
            });

            inputMenu.Items.Add(pasteItem);
            inputMenu.Items.Add(clearItem);
            inputTextBox.ContextMenuStrip = inputMenu;

            ContextMenuStrip resultMenu = new ContextMenuStrip();

            ToolStripMenuItem exportItem = new ToolStripMenuItem("Xuất thành file", null, (s, e) =>
            {
                ExportButton_Click(null, null);
            });

            ToolStripMenuItem clearResultItem = new ToolStripMenuItem("Làm sạch", null, (s, e) =>
            {
                resultTextBox.Clear();
                SetPlaceholder(resultTextBox, "Kết quả sẽ hiển thị ở đây...");
                isResultPlaceholder = true;
                UpdateLineCount();
            });

            resultMenu.Items.Add(exportItem);
            resultMenu.Items.Add(new ToolStripSeparator());
            resultMenu.Items.Add(clearResultItem);
            resultTextBox.ContextMenuStrip = resultMenu;
        }
        #endregion

        #region PLATEHOLDER VÀ PASTE HANDLER
        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                e.Handled = true;

                try
                {
                    string pastedText = Clipboard.GetText();
                    pastedText = pastedText.Replace("\r\n", "\n");
                    pastedText = pastedText.Replace("\r", "\n");
                    pastedText = pastedText.Trim();

                    SetTextWithIndent(inputTextBox, pastedText);
                    inputTextBox.ForeColor = Color.Black;
                    isInputPlaceholder = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi paste: {ex.Message}");
                }
            }
        }

        private void SetPlaceholder(RichTextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.ForeColor = Color.Gray;

            textBox.SelectAll();
            textBox.SelectionFont = new Font(textBox.Font, FontStyle.Italic);
            textBox.DeselectAll();

            textBox.GotFocus += (sender, e) =>
            {
                if ((textBox == inputTextBox && isInputPlaceholder) ||
                    (textBox == resultTextBox && isResultPlaceholder))
                {
                    textBox.Clear();
                    textBox.ForeColor = Color.Black;
                    textBox.SelectAll();
                    textBox.SelectionFont = new Font(textBox.Font, FontStyle.Regular);
                    textBox.DeselectAll();

                    if (textBox == inputTextBox)
                        isInputPlaceholder = false;
                    else
                        isResultPlaceholder = false;
                }
            };

            textBox.LostFocus += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                    textBox.SelectAll();
                    textBox.SelectionFont = new Font(textBox.Font, FontStyle.Italic);
                    textBox.DeselectAll();

                    if (textBox == inputTextBox)
                        isInputPlaceholder = true;
                    else
                        isResultPlaceholder = true;
                }
            };
        }

        // ✅ SỬA: Thêm tham số moveCursorToEnd
        private void InputTextBox_TextChanged(object sender, EventArgs e)
        {
            // ✅ THÊM: Bỏ qua nếu đang suppress event
            if (suppressTextChangedEvent)
                return;

            UpdateLineCount();

            textChangedTimer?.Dispose();
            textChangedTimer = new System.Threading.Timer(_ =>
            {
                this.Invoke(new Action(() =>
                {
                    if (inputTextBox.Lines.Length < 500)
                    {
                        ApplyAlternatingColors(inputTextBox);
                    }
                }));
            }, null, 200, Timeout.Infinite);
        }

        private void ResultTextBox_TextChanged(object sender, EventArgs e)
        {
            // ✅ THÊM: Bỏ qua nếu đang suppress event
            if (suppressTextChangedEvent)
                return;

            UpdateLineCount();
        }

        // ✅ SỬA: Thêm tham số để kiểm soát việc di chuyển con trỏ
        private void SetTextWithIndent(RichTextBox textBox, string text, bool moveCursorToEnd = true)
        {
            textBox.SuspendLayout();

            // Lưu vị trí con trỏ hiện tại
            int currentPosition = textBox.SelectionStart;
            int currentLength = textBox.SelectionLength;

            textBox.Text = text + Environment.NewLine;

            textBox.SelectAll();
            textBox.SelectionIndent = 0;
            textBox.SelectionRightIndent = 0;

            // Chỉ di chuyển con trỏ về cuối khi cần (paste mới)
            if (moveCursorToEnd)
            {
                textBox.Select(textBox.Text.Length, 0);
                textBox.ScrollToCaret();
            }
            else
            {
                // Khôi phục vị trí con trỏ cũ (nếu hợp lệ)
                if (currentPosition <= textBox.Text.Length)
                {
                    textBox.Select(currentPosition, Math.Min(currentLength, textBox.Text.Length - currentPosition));
                }
                else
                {
                    textBox.Select(0, 0);
                }
            }

            textBox.ResumeLayout();
        }
        #endregion

        #region TƯƠNG TÁC CLICK ĐỂ HIGHLIGHT
        private void InputTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (isInputPlaceholder)
                return;

            try
            {
                int clickPosition = inputTextBox.GetCharIndexFromPosition(e.Location);
                int lineIndex = inputTextBox.GetLineFromCharIndex(clickPosition);

                string[] lines = inputTextBox.Lines;

                if (lineIndex < 0 || lineIndex >= lines.Length)
                    return;

                string clickedLine = lines[lineIndex].Trim();

                if (string.IsNullOrWhiteSpace(clickedLine))
                    return;

                string key = ExtractKeyFromLine(clickedLine);

                if (string.IsNullOrWhiteSpace(key))
                    return;

                if (inputTextBox.Lines.Length > LARGE_DATA_THRESHOLD)
                {
                    HighlightLineWithColorAsync(inputTextBox, lineIndex, true);
                    HighlightResultLinesByKeyAsync(key);
                }
                else
                {
                    HighlightLineWithColor(inputTextBox, lineIndex, true);
                    HighlightResultLinesByKey(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi InputTextBox_MouseClick: {ex.Message}");
            }
        }

        private void ResultTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (isResultPlaceholder)
                return;

            try
            {
                int clickPosition = resultTextBox.GetCharIndexFromPosition(e.Location);
                int lineIndex = resultTextBox.GetLineFromCharIndex(clickPosition);

                string[] lines = resultTextBox.Lines;

                if (lineIndex < 0 || lineIndex >= lines.Length)
                    return;

                string clickedLine = lines[lineIndex].Trim();

                if (string.IsNullOrWhiteSpace(clickedLine))
                    return;

                string key = ExtractKeyFromLine(clickedLine);

                if (string.IsNullOrWhiteSpace(key))
                    return;

                if (resultTextBox.Lines.Length > LARGE_DATA_THRESHOLD)
                {
                    HighlightLineWithColorAsync(resultTextBox, lineIndex, false);
                    HighlightInputLinesByKeyAsync(key);
                }
                else
                {
                    HighlightLineWithColor(resultTextBox, lineIndex, false);
                    HighlightInputLinesByKey(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi ResultTextBox_MouseClick: {ex.Message}");
            }
        }

        private string ExtractKeyFromLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            line = line.Trim();

            if (line.Contains("\t"))
            {
                int tabIndex = line.IndexOf('\t');
                return line.Substring(0, tabIndex).Trim();
            }

            if (line.Contains("="))
            {
                int eqIndex = line.IndexOf('=');
                return line.Substring(0, eqIndex).Trim();
            }

            return null;
        }

        // ✅ SỬA: Thêm logic lưu và khôi phục vị trí con trỏ
        private void HighlightLineWithColor(RichTextBox textBox, int lineIndex, bool isInputBox)
        {
            try
            {
                if (lineIndex < 0 || lineIndex >= textBox.Lines.Length)
                    return;

                // ✅ THÊM: Tắt TextChanged event tạm thời
                suppressTextChangedEvent = true;

                // ✅ THÊM: Lưu vị trí con trỏ hiện tại
                int currentPosition = textBox.SelectionStart;
                int currentLength = textBox.SelectionLength;

                List<int> lastHighlightedLines = isInputBox ? lastHighlightedLinesInput : lastHighlightedLinesResult;

                foreach (int oldLine in lastHighlightedLines)
                {
                    if (oldLine >= 0 && oldLine < textBox.Lines.Length)
                    {
                        int oldStartIndex = textBox.GetFirstCharIndexFromLine(oldLine);
                        if (oldStartIndex >= 0)
                        {
                            textBox.Select(oldStartIndex, textBox.Lines[oldLine].Length);
                            textBox.SelectionBackColor = textBox.BackColor;
                        }
                    }
                }

                if (isInputBox)
                    lastHighlightedLinesInput.Clear();
                else
                    lastHighlightedLinesResult.Clear();

                int startIndex = textBox.GetFirstCharIndexFromLine(lineIndex);
                if (startIndex >= 0)
                {
                    textBox.Select(startIndex, textBox.Lines[lineIndex].Length);
                    textBox.SelectionBackColor = Color.Orange;

                    EnsureLineIsVisible(textBox, lineIndex);
                }

                // ✅ THÊM: Khôi phục vị trí con trỏ
                if (currentPosition <= textBox.Text.Length)
                {
                    textBox.Select(currentPosition, Math.Min(currentLength, textBox.Text.Length - currentPosition));
                }

                if (isInputBox)
                {
                    lastHighlightedLineInput = lineIndex;
                    lastHighlightedLinesInput.Add(lineIndex);
                }
                else
                {
                    lastHighlightedLineResult = lineIndex;
                    lastHighlightedLinesResult.Add(lineIndex);
                }

                // ✅ THÊM: Bật lại TextChanged event
                suppressTextChangedEvent = false;
            }
            catch (Exception ex)
            {
                suppressTextChangedEvent = false;
                System.Diagnostics.Debug.WriteLine($"Lỗi HighlightLineWithColor: {ex.Message}");
            }
        }

        private void EnsureLineIsVisible(RichTextBox textBox, int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= textBox.Lines.Length)
                return;

            SendMessage(textBox.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

            try
            {
                int visibleLines = GetVisibleLineCount(textBox);

                if (visibleLines <= 0) return;

                int targetLine = Math.Max(0, lineIndex - visibleLines / 2);

                int totalLines = textBox.Lines.Length;
                if (targetLine + visibleLines > totalLines)
                {
                    targetLine = Math.Max(0, totalLines - visibleLines);
                }

                int targetCharIndex = textBox.GetFirstCharIndexFromLine(targetLine);
                if (targetCharIndex >= 0)
                {
                    textBox.Select(targetCharIndex, 0);
                    textBox.ScrollToCaret();
                }
            }
            finally
            {
                SendMessage(textBox.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                textBox.Invalidate();
            }
        }

        private int GetVisibleLineCount(RichTextBox textBox)
        {
            using (Graphics g = textBox.CreateGraphics())
            {
                int clientHeight = textBox.ClientSize.Height - textBox.Padding.Top - textBox.Padding.Bottom;
                int lineHeight = TextRenderer.MeasureText(g, "A", textBox.Font).Height;

                if (lineHeight == 0) return 10;

                return clientHeight / lineHeight;
            }
        }

        private void HighlightLineWithColorAsync(RichTextBox textBox, int lineIndex, bool isInputBox)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (lineIndex >= 0 && lineIndex < textBox.Lines.Length)
                    {
                        this.Invoke(new Action(() =>
                        {
                            try
                            {
                                List<int> lastHighlightedLines = isInputBox ? lastHighlightedLinesInput : lastHighlightedLinesResult;

                                foreach (int oldLine in lastHighlightedLines)
                                {
                                    if (oldLine >= 0 && oldLine < textBox.Lines.Length)
                                    {
                                        int oldStartIndex = textBox.GetFirstCharIndexFromLine(oldLine);
                                        if (oldStartIndex >= 0)
                                        {
                                            textBox.Select(oldStartIndex, textBox.Lines[oldLine].Length);
                                            textBox.SelectionBackColor = Color.White;
                                        }
                                    }
                                }

                                if (isInputBox)
                                    lastHighlightedLinesInput.Clear();
                                else
                                    lastHighlightedLinesResult.Clear();

                                int startIndex = textBox.GetFirstCharIndexFromLine(lineIndex);
                                if (startIndex >= 0)
                                {
                                    textBox.Select(startIndex, textBox.Lines[lineIndex].Length);
                                    textBox.SelectionBackColor = Color.Yellow;
                                    textBox.ScrollToCaret();
                                }

                                if (isInputBox)
                                {
                                    lastHighlightedLineInput = lineIndex;
                                    lastHighlightedLinesInput.Add(lineIndex);
                                }
                                else
                                {
                                    lastHighlightedLineResult = lineIndex;
                                    lastHighlightedLinesResult.Add(lineIndex);
                                }
                            }
                            catch { }
                        }));
                    }
                }
                catch { }
            });
        }

        private void HighlightResultLinesByKey(string key)
        {
            try
            {
                if (isResultPlaceholder)
                    return;

                string[] resultLines = resultTextBox.Lines;
                List<int> matchingLines = new List<int>();

                for (int i = 0; i < resultLines.Length; i++)
                {
                    string lineKey = ExtractKeyFromLine(resultLines[i]);
                    if (!string.IsNullOrWhiteSpace(lineKey) && lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingLines.Add(i);
                    }
                }

                foreach (int oldLine in lastHighlightedLinesResult)
                {
                    if (oldLine >= 0 && oldLine < resultTextBox.Lines.Length)
                    {
                        int oldStartIndex = resultTextBox.GetFirstCharIndexFromLine(oldLine);
                        if (oldStartIndex >= 0)
                        {
                            resultTextBox.Select(oldStartIndex, resultTextBox.Lines[oldLine].Length);
                            resultTextBox.SelectionBackColor = resultTextBox.BackColor;
                        }
                    }
                }

                if (matchingLines.Count > 0)
                {
                    foreach (int lineIndex in matchingLines)
                    {
                        int startIndex = resultTextBox.GetFirstCharIndexFromLine(lineIndex);
                        if (startIndex >= 0)
                        {
                            resultTextBox.Select(startIndex, resultTextBox.Lines[lineIndex].Length);
                            resultTextBox.SelectionBackColor = Color.Orange;
                        }
                    }

                    EnsureLineIsVisible(resultTextBox, matchingLines[0]);
                    lastHighlightedLineResult = matchingLines[0];
                    lastHighlightedLinesResult = new List<int>(matchingLines);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi HighlightResultLinesByKey: {ex.Message}");
            }
        }

        private void HighlightResultLinesByKeyAsync(string key)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (isResultPlaceholder)
                        return;

                    string[] resultLines = resultTextBox.Lines;
                    List<int> matchingLines = new List<int>();

                    for (int i = 0; i < resultLines.Length; i++)
                    {
                        string lineKey = ExtractKeyFromLine(resultLines[i]);
                        if (!string.IsNullOrWhiteSpace(lineKey) && lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            matchingLines.Add(i);
                        }
                    }

                    if (matchingLines.Count > 0)
                    {
                        this.Invoke(new Action(() =>
                        {
                            try
                            {
                                foreach (int oldLine in lastHighlightedLinesResult)
                                {
                                    if (oldLine >= 0 && oldLine < resultTextBox.Lines.Length)
                                    {
                                        int oldStartIndex = resultTextBox.GetFirstCharIndexFromLine(oldLine);
                                        if (oldStartIndex >= 0)
                                        {
                                            resultTextBox.Select(oldStartIndex, resultTextBox.Lines[oldLine].Length);
                                            resultTextBox.SelectionBackColor = Color.White;
                                        }
                                    }
                                }

                                foreach (int lineIndex in matchingLines)
                                {
                                    int startIndex = resultTextBox.GetFirstCharIndexFromLine(lineIndex);
                                    if (startIndex >= 0)
                                    {
                                        resultTextBox.Select(startIndex, resultTextBox.Lines[lineIndex].Length);
                                        resultTextBox.SelectionBackColor = Color.Yellow;
                                    }
                                }

                                int firstLineStart = resultTextBox.GetFirstCharIndexFromLine(matchingLines[0]);
                                if (firstLineStart >= 0)
                                {
                                    resultTextBox.Select(firstLineStart, 0);
                                    resultTextBox.ScrollToCaret();
                                }

                                lastHighlightedLineResult = matchingLines[0];
                                lastHighlightedLinesResult = new List<int>(matchingLines);
                            }
                            catch { }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi HighlightResultLinesByKeyAsync: {ex.Message}");
                }
            });
        }

        private void HighlightInputLinesByKey(string key)
        {
            try
            {
                if (isInputPlaceholder)
                    return;

                string[] inputLines = inputTextBox.Lines;
                List<int> matchingLines = new List<int>();

                for (int i = 0; i < inputLines.Length; i++)
                {
                    string lineKey = ExtractKeyFromLine(inputLines[i]);
                    if (!string.IsNullOrWhiteSpace(lineKey) && lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingLines.Add(i);
                    }
                }

                foreach (int oldLine in lastHighlightedLinesInput)
                {
                    if (oldLine >= 0 && oldLine < inputTextBox.Lines.Length)
                    {
                        int oldStartIndex = inputTextBox.GetFirstCharIndexFromLine(oldLine);
                        if (oldStartIndex >= 0)
                        {
                            inputTextBox.Select(oldStartIndex, inputTextBox.Lines[oldLine].Length);
                            inputTextBox.SelectionBackColor = inputTextBox.BackColor;
                        }
                    }
                }

                if (matchingLines.Count > 0)
                {
                    foreach (int lineIndex in matchingLines)
                    {
                        int startIndex = inputTextBox.GetFirstCharIndexFromLine(lineIndex);
                        if (startIndex >= 0)
                        {
                            inputTextBox.Select(startIndex, inputTextBox.Lines[lineIndex].Length);
                            inputTextBox.SelectionBackColor = Color.Orange;
                        }
                    }

                    EnsureLineIsVisible(inputTextBox, matchingLines[0]);
                    lastHighlightedLineInput = matchingLines[0];
                    lastHighlightedLinesInput = new List<int>(matchingLines);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi HighlightInputLinesByKey: {ex.Message}");
            }
        }

        private void HighlightInputLinesByKeyAsync(string key)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (isInputPlaceholder)
                        return;

                    string[] inputLines = inputTextBox.Lines;
                    List<int> matchingLines = new List<int>();

                    for (int i = 0; i < inputLines.Length; i++)
                    {
                        string lineKey = ExtractKeyFromLine(inputLines[i]);
                        if (!string.IsNullOrWhiteSpace(lineKey) && lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            matchingLines.Add(i);
                        }
                    }

                    if (matchingLines.Count > 0)
                    {
                        this.Invoke(new Action(() =>
                        {
                            try
                            {
                                foreach (int oldLine in lastHighlightedLinesInput)
                                {
                                    if (oldLine >= 0 && oldLine < inputTextBox.Lines.Length)
                                    {
                                        int oldStartIndex = inputTextBox.GetFirstCharIndexFromLine(oldLine);
                                        if (oldStartIndex >= 0)
                                        {
                                            inputTextBox.Select(oldStartIndex, inputTextBox.Lines[oldLine].Length);
                                            inputTextBox.SelectionBackColor = Color.White;
                                        }
                                    }
                                }

                                foreach (int lineIndex in matchingLines)
                                {
                                    int startIndex = inputTextBox.GetFirstCharIndexFromLine(lineIndex);
                                    if (startIndex >= 0)
                                    {
                                        inputTextBox.Select(startIndex, inputTextBox.Lines[lineIndex].Length);
                                        inputTextBox.SelectionBackColor = Color.Yellow;
                                    }
                                }

                                int firstLineStart = inputTextBox.GetFirstCharIndexFromLine(matchingLines[0]);
                                if (firstLineStart >= 0)
                                {
                                    inputTextBox.Select(firstLineStart, 0);
                                    inputTextBox.ScrollToCaret();
                                }

                                lastHighlightedLineInput = matchingLines[0];
                                lastHighlightedLinesInput = new List<int>(matchingLines);
                            }
                            catch { }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi HighlightInputLinesByKeyAsync: {ex.Message}");
                }
            });
        }
        #endregion

        #region CẬP NHẬT SỐ DÒNG
        private void UpdateLineCount()
        {
            int inputLines = 0;
            if (!isInputPlaceholder && !string.IsNullOrWhiteSpace(inputTextBox.Text))
            {
                inputLines = inputTextBox.Lines.Count(line => !string.IsNullOrWhiteSpace(line));
            }

            int resultLines = 0;
            if (!isResultPlaceholder && !string.IsNullOrWhiteSpace(resultTextBox.Text))
            {
                resultLines = resultTextBox.Lines.Count(line => !string.IsNullOrWhiteSpace(line));
            }

            inputCountLabel.Text = $"Số lượng nhập vào: {inputLines}";
            resultCountLabel.Text = $"Số lượng kết quả: {resultLines}";
        }
        #endregion

        #region XỬ LÝ LỌC DỮ LIỆU
        private void ApplyAlternatingColors(RichTextBox RichTextBox)
        {
            if (RichTextBox is null)
            {
                throw new ArgumentNullException(nameof(RichTextBox));
            }
            return;
        }

        private System.Threading.Timer textChangedTimer;

        private void FilterButton_Click(object sender, EventArgs e)
        {
            if (filterWorker.IsBusy)
            {
                MessageBox.Show("Đang xử lý dữ liệu, vui lòng đợi...", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            filterButton.Enabled = false;
            filterButton.Text = "Đang xử lý...";
            Cursor = Cursors.WaitCursor;

            int lineCount = lineCountComboBox.SelectedIndex == 0 ? 1 :
                            lineCountComboBox.SelectedIndex == 1 ? 2 : 3;

            string[] lines = inputTextBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            filterWorker.RunWorkerAsync(new { Lines = lines, LineCount = lineCount });
        }

        private void FilterWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as dynamic;
            string[] lines = args.Lines;
            int lineCount = args.LineCount;

            var result = FilterData(lines, lineCount);
            e.Result = result;
        }

        private void FilterWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            filterButton.Enabled = true;
            filterButton.Text = "LỌC DỮ LIỆU";
            Cursor = Cursors.Default;

            if (e.Error != null)
            {
                MessageBox.Show($"Lỗi khi lọc dữ liệu: {e.Error.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (e.Result is List<string> filteredData && filteredData.Count > 0)
            {
                resultTextBox.SuspendLayout();
                resultTextBox.Clear();
                resultTextBox.ForeColor = Color.Black;
                isResultPlaceholder = false;

                string resultText = string.Join(Environment.NewLine, filteredData);
                SetTextWithIndent(resultTextBox, resultText);

                ApplyAlternatingColors(resultTextBox);
                resultTextBox.ResumeLayout();
            }
            UpdateLineCount();
        }
        #endregion

        #region NÚT CLEAR VÀ EXPORT THÀNH FILE
        private void ClearButton_Click(object sender, EventArgs e)
        {
            inputTextBox.Clear();
            resultTextBox.Clear();
            SetPlaceholder(inputTextBox, "Paste dữ liệu vào đây...");
            SetPlaceholder(resultTextBox, "Kết quả sẽ hiển thị ở đây...");
            isInputPlaceholder = true;
            isResultPlaceholder = true;
            UpdateLineCount();
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            if (isResultPlaceholder || string.IsNullOrWhiteSpace(resultTextBox.Text))
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                saveDialog.DefaultExt = "txt";
                saveDialog.FileName = $"TOA_DO_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                saveDialog.Title = "Xuất dữ liệu ra file";

                string defaultFolder = @"D:\Non_Documents";

                if (!System.IO.Directory.Exists(defaultFolder))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(defaultFolder);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể tạo thư mục: {ex.Message}",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        defaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                }

                saveDialog.InitialDirectory = defaultFolder;

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string textToSave = resultTextBox.Text;
                        textToSave = textToSave.Replace("\n", Environment.NewLine);

                        File.WriteAllText(saveDialog.FileName, textToSave, Encoding.UTF8);

                        ShowExportSuccessMessage(saveDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xuất file: {ex.Message}",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowExportSuccessMessage(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            using (var customForm = new System.Windows.Forms.Form())
            {
                customForm.Text = "Xuất File Thành Công";
                customForm.Size = new System.Drawing.Size(350, 200);
                customForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                customForm.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                customForm.MaximizeBox = false;
                customForm.MinimizeBox = false;
                customForm.ShowInTaskbar = false;

                var label1 = new System.Windows.Forms.Label
                {
                    Text = "Đã xuất file thành công!",
                    Location = new System.Drawing.Point(20, 15),
                    Size = new System.Drawing.Size(400, 20),
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
                };

                var label2 = new System.Windows.Forms.Label
                {
                    Text = "Tên file:",
                    Location = new System.Drawing.Point(20, 45),
                    Size = new System.Drawing.Size(65, 20),
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
                };

                var fileNameLabel = new System.Windows.Forms.Label
                {
                    Text = fileName,
                    Location = new System.Drawing.Point(80, 45),
                    Size = new System.Drawing.Size(350, 20),
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                    ForeColor = System.Drawing.Color.Green
                };

                var label3 = new System.Windows.Forms.Label
                {
                    Text = "Bạn có muốn mở thư mục chứa file không?",
                    Location = new System.Drawing.Point(20, 75),
                    Size = new System.Drawing.Size(400, 20),
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
                };

                var yesButton = new System.Windows.Forms.Button
                {
                    Text = "&Có",
                    Location = new System.Drawing.Point(80, 120),
                    Size = new System.Drawing.Size(80, 30),
                    DialogResult = System.Windows.Forms.DialogResult.Yes,
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
                };

                var noButton = new System.Windows.Forms.Button
                {
                    Text = "&Không",
                    Location = new System.Drawing.Point(180, 120),
                    Size = new System.Drawing.Size(80, 30),
                    DialogResult = System.Windows.Forms.DialogResult.No,
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
                };

                yesButton.Click += (s, e) => { customForm.DialogResult = System.Windows.Forms.DialogResult.Yes; };
                noButton.Click += (s, e) => { customForm.DialogResult = System.Windows.Forms.DialogResult.No; };

                customForm.Controls.Add(label1);
                customForm.Controls.Add(label2);
                customForm.Controls.Add(fileNameLabel);
                customForm.Controls.Add(label3);
                customForm.Controls.Add(yesButton);
                customForm.Controls.Add(noButton);
                customForm.AcceptButton = yesButton;
                customForm.CancelButton = noButton;

                var result = customForm.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                    }
                    catch (Exception ex)
                    {
                        ShowSilentError($"Không thể mở thư mục: {ex.Message}");
                    }
                }
            }
        }

        private void ShowSilentError(string message)
        {
            using (var errorForm = new System.Windows.Forms.Form())
            {
                errorForm.Text = "Lỗi";
                errorForm.Size = new System.Drawing.Size(350, 150);
                errorForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                errorForm.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                errorForm.MaximizeBox = false;
                errorForm.MinimizeBox = false;
                errorForm.ShowInTaskbar = false;

                var label = new System.Windows.Forms.Label
                {
                    Text = message,
                    Location = new System.Drawing.Point(20, 20),
                    Size = new System.Drawing.Size(300, 50),
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft
                };

                var okButton = new System.Windows.Forms.Button
                {
                    Text = "OK",
                    Location = new System.Drawing.Point(130, 80),
                    Size = new System.Drawing.Size(80, 30),
                    DialogResult = System.Windows.Forms.DialogResult.OK
                };

                okButton.Click += (s, e) => { errorForm.Close(); };
                errorForm.Controls.Add(label);
                errorForm.Controls.Add(okButton);
                errorForm.AcceptButton = okButton;
                errorForm.ShowDialog();
            }
        }
        #endregion

        #region PHƯƠNG THỨC LỌC DỮ LIỆU CHÍNH
        private List<string> FilterData(string[] lines, int lineCount)
        {
            var result = new List<string>();

            bool isTabFormat = false;
            if (lines.Length > 0)
            {
                isTabFormat = lines[0].Contains("\t");
            }

            var groups = isTabFormat
                ? lines
                    .Select(line => line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries))
                    .Where(parts => parts.Length == 3)
                    .GroupBy(
                        parts => parts[0],
                        parts =>
                        {
                            int.TryParse(parts[1], out int val1);
                            int.TryParse(parts[2], out int val2);
                            return new int[] { val1, val2 };
                        }
                    )
                : lines
                    .Select(line => line.Split('='))
                    .Where(parts => parts.Length == 2)
                    .GroupBy(
                        parts => parts[0],
                        parts =>
                        {
                            var values = parts[1].Split(',');
                            int.TryParse(values[0], out int val1);
                            int.TryParse(values[1], out int val2);
                            return new int[] { val1, val2 };
                        }
                    );

            foreach (var group in groups)
            {
                var key = group.Key;
                var values = group.ToList();

                if (values.Count == 1)
                {
                    if (isTabFormat)
                        result.Add($"{key}\t{values[0][0]}\t{values[0][1]}");
                    else
                        result.Add($"{key}={values[0][0]},{values[0][1]}");
                    continue;
                }

                var selectedIndices = SelectDifferentLines(values, lineCount);

                foreach (var idx in selectedIndices)
                {
                    if (isTabFormat)
                        result.Add($"{key}\t{values[idx][0]}\t{values[idx][1]}");
                    else
                        result.Add($"{key}={values[idx][0]},{values[idx][1]}");
                }
            }

            return result;
        }

        private List<int> SelectDifferentLines(List<int[]> values, int lineCount)
        {
            if (lineCount == 1)
            {
                var distances = new List<(int index, double maxDist)>();

                for (int i = 0; i < values.Count; i++)
                {
                    double currentMax = 0.0;
                    for (int j = 0; j < values.Count; j++)
                    {
                        if (i != j)
                        {
                            double distance = Math.Sqrt(
                                Math.Pow(values[i][0] - values[j][0], 2) +
                                Math.Pow(values[i][1] - values[j][1], 2)
                            );
                            if (distance > currentMax)
                            {
                                currentMax = distance;
                            }
                        }
                    }
                    distances.Add((i, currentMax));
                }

                return distances
                    .OrderByDescending(d => d.maxDist)
                    .Take(1)
                    .Select(d => d.index)
                    .ToList();
            }

            var selected = new List<int>();
            var remaining = Enumerable.Range(0, values.Count).ToList();

            double maxAvgDist = -1;
            int firstIndex = 0;

            foreach (var i in remaining)
            {
                double totalDist = 0;
                int count = 0;

                foreach (var j in remaining)
                {
                    if (i != j)
                    {
                        double distance = Math.Sqrt(
                            Math.Pow(values[i][0] - values[j][0], 2) +
                            Math.Pow(values[i][1] - values[j][1], 2)
                        );
                        totalDist += distance;
                        count++;
                    }
                }

                double avgDist = count > 0 ? totalDist / count : 0;
                if (avgDist > maxAvgDist)
                {
                    maxAvgDist = avgDist;
                    firstIndex = i;
                }
            }

            selected.Add(firstIndex);
            remaining.Remove(firstIndex);

            double minDistanceThreshold = 5.0;

            while (selected.Count < lineCount && remaining.Count > 0)
            {
                double maxMinDist = -1;
                int nextIndex = -1;

                foreach (var i in remaining)
                {
                    double minDist = double.MaxValue;

                    foreach (var selectedIdx in selected)
                    {
                        double distance = Math.Sqrt(
                            Math.Pow(values[i][0] - values[selectedIdx][0], 2) +
                            Math.Pow(values[i][1] - values[selectedIdx][1], 2)
                        );

                        if (distance < minDist)
                        {
                            minDist = distance;
                        }
                    }

                    if (minDist > maxMinDist)
                    {
                        maxMinDist = minDist;
                        nextIndex = i;
                    }
                }

                if (nextIndex != -1 && maxMinDist >= minDistanceThreshold)
                {
                    selected.Add(nextIndex);
                    remaining.Remove(nextIndex);
                }
                else
                {
                    break;
                }
            }

            return selected.OrderBy(idx => idx).ToList();
        }
        #endregion
    }

    #region CLASS MỞ RỘNG
    public static class RichTextBoxExtensions
    {
        public static int GetFirstVisibleLineIndex(this RichTextBox rtb)
        {
            return rtb.GetLineFromCharIndex(rtb.GetCharIndexFromPosition(new Point(0, 0)));
        }
    }

    public class CustomRichTextBox : RichTextBox
    {
        public CustomRichTextBox()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer, true);
        }
    }
    #endregion
}