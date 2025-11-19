using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DATAFILTER
{
    public partial class MainForm : Form
    {
        private readonly Color evenRowColor = Color.White;
        private readonly Color oddRowColor = Color.FromArgb(220, 220, 220); // Xám nhạt
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;
        private readonly BackgroundWorker filterWorker;
        private int lastHighlightedLineInput = -1;
        private int lastHighlightedLineResult = -1;
        private const int LARGE_DATA_THRESHOLD = 5000;

        #region CONSTRUCTOR AND INITIALIZATION
        public MainForm()
        {
            InitializeComponent();

            // Tắt tự động xuống dòng
            inputTextBox.WordWrap = false;
            resultTextBox.WordWrap = false;
            inputTextBox.ScrollBars = RichTextBoxScrollBars.Both;
            resultTextBox.ScrollBars = RichTextBoxScrollBars.Both;

            // Bỏ Border
            inputTextBox.BorderStyle = BorderStyle.None;
            resultTextBox.BorderStyle = BorderStyle.None;

            // Tắt context menu mặc định
            inputTextBox.ContextMenuStrip = null;
            resultTextBox.ContextMenuStrip = null;
          
            SetPlaceholder(inputTextBox, "Paste dữ liệu vào đây...");
            SetPlaceholder(resultTextBox, "Kết quả sẽ hiển thị ở đây...");

            // Hook vào paste event
            inputTextBox.KeyDown += InputTextBox_KeyDown;

            // Tạo context menu
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
            // Context menu cho inputTextBox
            ContextMenuStrip inputMenu = new ContextMenuStrip();

            ToolStripMenuItem pasteItem = new ToolStripMenuItem("Dán từ cliboard", null, (s, e) =>
            {
                try
                {
                    string pastedText = Clipboard.GetText();
                    pastedText = pastedText.Replace("\r\n", "\n");
                    pastedText = pastedText.Replace("\r", "\n");
                    pastedText = pastedText.Trim();
                    inputTextBox.Text = pastedText;
                    inputTextBox.ForeColor = Color.Black;
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
                UpdateLineCount();
            });

            inputMenu.Items.Add(pasteItem);
            inputMenu.Items.Add(clearItem);
            inputTextBox.ContextMenuStrip = inputMenu;

            // Context menu cho resultTextBox
            ContextMenuStrip resultMenu = new ContextMenuStrip();

            ToolStripMenuItem exportItem = new ToolStripMenuItem("Xuất thành file", null, (s, e) =>
            {
                ExportButton_Click(null, null);
            });

            ToolStripMenuItem clearResultItem = new ToolStripMenuItem("Làm sạch", null, (s, e) =>
            {
                resultTextBox.Clear();
                SetPlaceholder(resultTextBox, "Kết quả sẽ hiển thị ở đây...");
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

                    // Loại bỏ ký tự không cần thiết
                    pastedText = pastedText.Replace("\r\n", "\n");  // Normalize newlines
                    pastedText = pastedText.Replace("\r", "\n");
                    pastedText = pastedText.Trim();

                    inputTextBox.Text = pastedText;
                    inputTextBox.ForeColor = Color.Black;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi paste: {ex.Message}");
                }
            }
        }
        /*
        private void SetPlaceholder(RichTextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.ForeColor = Color.Gray;
            textBox.GotFocus += (sender, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.ForeColor = Color.Black;
                }
            };
            textBox.LostFocus += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                }
            };
        }
        */
        private void SetPlaceholder(RichTextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.ForeColor = Color.Gray;

            // Áp dụng font italic cho placeholder
            textBox.SelectAll();
            textBox.SelectionFont = new Font(textBox.Font, FontStyle.Italic);
            textBox.DeselectAll();

            textBox.GotFocus += (sender, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Clear();
                    textBox.ForeColor = Color.Black;
                    // Reset font về bình thường khi focus
                    textBox.SelectAll();
                    textBox.SelectionFont = new Font(textBox.Font, FontStyle.Regular);
                    textBox.DeselectAll();
                }
            };

            textBox.LostFocus += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                    // Áp dụng font italic lại khi mất focus
                    textBox.SelectAll();
                    textBox.SelectionFont = new Font(textBox.Font, FontStyle.Italic);
                    textBox.DeselectAll();
                }
            };
        }
        #endregion

        #region TƯƠNG TÁC CLICK ĐỂ HIGHLIGHT
        // ============================================
        // PHƯƠNG THỨC XỬ LÝ CLICK TRÊN INPUT TEXT BOX
        // ============================================
        private void InputTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            // Bỏ qua nếu đang hiển thị placeholder
            if (inputTextBox.ForeColor == Color.Gray)
                return;

            try
            {
                // Lấy vị trí dòng được click
                int clickPosition = inputTextBox.GetCharIndexFromPosition(e.Location);
                int lineIndex = inputTextBox.GetLineFromCharIndex(clickPosition);

                string[] lines = inputTextBox.Lines;

                // Kiểm tra dòng hợp lệ
                if (lineIndex < 0 || lineIndex >= lines.Length)
                    return;

                string clickedLine = lines[lineIndex].Trim();

                // Bỏ qua nếu dòng trống
                if (string.IsNullOrWhiteSpace(clickedLine))
                    return;

                // Trích xuất key từ dòng được click
                string key = ExtractKeyFromLine(clickedLine);

                if (string.IsNullOrWhiteSpace(key))
                    return;

                // Nếu dữ liệu lớn, chỉ highlight trong thread background
                if (inputTextBox.Lines.Length > LARGE_DATA_THRESHOLD)
                {
                    // Highlight không đồng bộ
                    HighlightLineWithColorAsync(inputTextBox, lineIndex, true);
                    HighlightResultLinesByKeyAsync(key);
                }
                else
                {
                    // Dữ liệu nhỏ, highlight đồng bộ (nhanh hơn)
                    HighlightLineWithColor(inputTextBox, lineIndex, true);
                    HighlightResultLinesByKey(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi InputTextBox_MouseClick: {ex.Message}");
            }
        }

        // ============================================
        // PHƯƠNG THỨC XỬ LÝ CLICK TRÊN RESULT TEXT BOX
        // ============================================
        private void ResultTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            // Bỏ qua nếu đang hiển thị placeholder
            if (resultTextBox.ForeColor == Color.Gray)
                return;

            try
            {
                // Lấy vị trí dòng được click
                int clickPosition = resultTextBox.GetCharIndexFromPosition(e.Location);
                int lineIndex = resultTextBox.GetLineFromCharIndex(clickPosition);

                string[] lines = resultTextBox.Lines;

                // Kiểm tra dòng hợp lệ
                if (lineIndex < 0 || lineIndex >= lines.Length)
                    return;

                string clickedLine = lines[lineIndex].Trim();

                // Bỏ qua nếu dòng trống
                if (string.IsNullOrWhiteSpace(clickedLine))
                    return;

                // Trích xuất key từ dòng được click
                string key = ExtractKeyFromLine(clickedLine);

                if (string.IsNullOrWhiteSpace(key))
                    return;

                // Nếu dữ liệu lớn, chỉ highlight trong thread background
                if (resultTextBox.Lines.Length > LARGE_DATA_THRESHOLD)
                {
                    // Highlight không đồng bộ
                    HighlightLineWithColorAsync(resultTextBox, lineIndex, false);
                    HighlightInputLinesByKeyAsync(key);
                }
                else
                {
                    // Dữ liệu nhỏ, highlight đồng bộ (nhanh hơn)
                    HighlightLineWithColor(resultTextBox, lineIndex, false);
                    HighlightInputLinesByKey(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi ResultTextBox_MouseClick: {ex.Message}");
            }
        }

        // ============================================
        // PHƯƠNG THỨC TRÍCH XUẤT KEY TỪ DÒNG
        // ============================================
        private string ExtractKeyFromLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            line = line.Trim();

            // Kiểm tra định dạng tab (key\tval1\tval2)
            if (line.Contains("\t"))
            {
                int tabIndex = line.IndexOf('\t');
                return line.Substring(0, tabIndex).Trim();
            }

            // Kiểm tra định dạng = (key=val1,val2)
            if (line.Contains("="))
            {
                int eqIndex = line.IndexOf('=');
                return line.Substring(0, eqIndex).Trim();
            }

            return null;
        }

        // ============================================
        // PHƯƠNG THỨC HIGHLIGHT MỘT DÒNG BẰNG TÔ MÀU NỀN (Đồng bộ)
        // ============================================
        private void HighlightLineWithColor(RichTextBox textBox, int lineIndex, bool isInputBox)
        {
            try
            {
                if (lineIndex < 0 || lineIndex >= textBox.Lines.Length)
                    return;

                // Lấy lastHighlightedLine tương ứng
                int lastHighlightedLine = isInputBox ? lastHighlightedLineInput : lastHighlightedLineResult;

                // Clear highlight cũ
                if (lastHighlightedLine >= 0 && lastHighlightedLine < textBox.Lines.Length)
                {
                    int oldStartIndex = textBox.GetFirstCharIndexFromLine(lastHighlightedLine);
                    if (oldStartIndex >= 0)
                    {
                        textBox.Select(oldStartIndex, textBox.Lines[lastHighlightedLine].Length);
                        textBox.SelectionBackColor = Color.White;
                    }
                }

                // Highlight dòng mới
                int startIndex = textBox.GetFirstCharIndexFromLine(lineIndex);
                if (startIndex >= 0)
                {
                    textBox.Select(startIndex, textBox.Lines[lineIndex].Length);
                    textBox.SelectionBackColor = Color.Orange;
                    textBox.ScrollToCaret();
                }

                // Cập nhật lastHighlightedLine
                if (isInputBox)
                    lastHighlightedLineInput = lineIndex;
                else
                    lastHighlightedLineResult = lineIndex;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi HighlightLineWithColor: {ex.Message}");
            }
        }

        // ============================================
        // PHƯƠNG THỨC HIGHLIGHT MỘT DÒNG BẰNG TÔ MÀU NỀN (Không đồng bộ)
        // ============================================
        private void HighlightLineWithColorAsync(RichTextBox textBox, int lineIndex, bool isInputBox)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // Tìm dòng cần highlight
                    if (lineIndex >= 0 && lineIndex < textBox.Lines.Length)
                    {
                        // Invoke về UI thread để thực hiện highlight
                        this.Invoke(new Action(() =>
                        {
                            try
                            {
                                // Lấy lastHighlightedLine tương ứng
                                int lastHighlightedLine = isInputBox ? lastHighlightedLineInput : lastHighlightedLineResult;

                                // Clear highlight cũ
                                if (lastHighlightedLine >= 0 && lastHighlightedLine < textBox.Lines.Length)
                                {
                                    int oldStartIndex = textBox.GetFirstCharIndexFromLine(lastHighlightedLine);
                                    if (oldStartIndex >= 0)
                                    {
                                        textBox.Select(oldStartIndex, textBox.Lines[lastHighlightedLine].Length);
                                        textBox.SelectionBackColor = Color.White;
                                    }
                                }

                                // Highlight dòng mới
                                int startIndex = textBox.GetFirstCharIndexFromLine(lineIndex);
                                if (startIndex >= 0)
                                {
                                    textBox.Select(startIndex, textBox.Lines[lineIndex].Length);
                                    textBox.SelectionBackColor = Color.Yellow;
                                    textBox.ScrollToCaret();
                                }

                                // Cập nhật lastHighlightedLine
                                if (isInputBox)
                                    lastHighlightedLineInput = lineIndex;
                                else
                                    lastHighlightedLineResult = lineIndex;
                            }
                            catch { }
                        }));
                    }
                }
                catch { }
            });
        }

        // ============================================
        // PHƯƠNG THỨC TÌM VÀ HIGHLIGHT CÁC DÒNG CÓ CÙNG KEY TRONG RESULT (Đồng bộ)
        // ============================================
        private void HighlightResultLinesByKey(string key)
        {
            try
            {
                // Bỏ qua nếu result đang hiển thị placeholder
                if (resultTextBox.ForeColor == Color.Gray)
                    return;

                string[] resultLines = resultTextBox.Lines;

                for (int i = 0; i < resultLines.Length; i++)
                {
                    string lineKey = ExtractKeyFromLine(resultLines[i]);
                    if (!string.IsNullOrWhiteSpace(lineKey) && lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        HighlightLineWithColor(resultTextBox, i, false);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi HighlightResultLinesByKey: {ex.Message}");
            }
        }

        // ============================================
        // PHƯƠNG THỨC TÌM VÀ HIGHLIGHT CÁC DÒNG CÓ CÙNG KEY TRONG RESULT (Không đồng bộ)
        // ============================================
        private void HighlightResultLinesByKeyAsync(string key)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // Bỏ qua nếu result đang hiển thị placeholder
                    if (resultTextBox.ForeColor == Color.Gray)
                        return;

                    string[] resultLines = resultTextBox.Lines;

                    for (int i = 0; i < resultLines.Length; i++)
                    {
                        string lineKey = ExtractKeyFromLine(resultLines[i]);
                        if (!string.IsNullOrWhiteSpace(lineKey) && lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            HighlightLineWithColorAsync(resultTextBox, i, false);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi HighlightResultLinesByKeyAsync: {ex.Message}");
                }
            });
        }

        // ============================================
        // PHƯƠNG THỨC TÌM VÀ HIGHLIGHT CÁC DÒNG CÓ CÙNG KEY TRONG INPUT (Đồng bộ)
        // ============================================
        private void HighlightInputLinesByKey(string key)
        {
            try
            {
                // Bỏ qua nếu input đang hiển thị placeholder
                if (inputTextBox.ForeColor == Color.Gray)
                    return;

                string[] inputLines = inputTextBox.Lines;

                for (int i = 0; i < inputLines.Length; i++)
                {
                    string lineKey = ExtractKeyFromLine(inputLines[i]);
                    if (!string.IsNullOrWhiteSpace(lineKey) && lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        HighlightLineWithColor(inputTextBox, i, true);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi HighlightInputLinesByKey: {ex.Message}");
            }
        }

        // ============================================
        // PHƯƠNG THỨC TÌM VÀ HIGHLIGHT CÁC DÒNG CÓ CÙNG KEY TRONG INPUT (Không đồng bộ)
        // ============================================
        private void HighlightInputLinesByKeyAsync(string key)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // Bỏ qua nếu input đang hiển thị placeholder
                    if (inputTextBox.ForeColor == Color.Gray)
                        return;

                    string[] inputLines = inputTextBox.Lines;

                    for (int i = 0; i < inputLines.Length; i++)
                    {
                        string lineKey = ExtractKeyFromLine(inputLines[i]);
                        if (!string.IsNullOrWhiteSpace(lineKey) && lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            HighlightLineWithColorAsync(inputTextBox, i, true);
                            return;
                        }
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
            // Đếm số dòng input (bỏ qua placeholder)
            int inputLines = 0;
            if (!string.IsNullOrWhiteSpace(inputTextBox.Text) &&
                inputTextBox.Text != "Paste dữ liệu vào đây..." &&
                inputTextBox.ForeColor != Color.Gray)
            {
                inputLines = inputTextBox.Lines.Count(line => !string.IsNullOrWhiteSpace(line));
            }

            // Đếm số dòng result (bỏ qua placeholder)
            int resultLines = 0;
            if (!string.IsNullOrWhiteSpace(resultTextBox.Text) &&
                resultTextBox.Text != "Kết quả sẽ hiển thị ở đây..." &&
                resultTextBox.ForeColor != Color.Gray)
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
            // Tắt chức năng vẽ màu nền xen kẽ
            return;
        }

        private System.Threading.Timer textChangedTimer;

        private void InputTextBox_TextChanged(object sender, EventArgs e)
        {
            // Cập nhật số lượng ngay lập tức (nhẹ)
            UpdateLineCount();

            // Delay việc áp dụng màu
            textChangedTimer?.Dispose();
            textChangedTimer = new System.Threading.Timer(_ =>
            {
                this.Invoke(new Action(() =>
                {
                    if (inputTextBox.Lines.Length < 500) // Chỉ áp dụng màu nếu ít dòng
                    {
                        ApplyAlternatingColors(inputTextBox);
                    }
                }));
            }, null, 200, Timeout.Infinite);
        }

        private void FilterButton_Click(object sender, EventArgs e)
        {
            if (filterWorker.IsBusy)
            {
                MessageBox.Show("Đang xử lý dữ liệu, vui lòng đợi...", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Hiển thị trạng thái loading đơn giản
            filterButton.Enabled = false;
            filterButton.Text = "Đang xử lý...";
            Cursor = Cursors.WaitCursor;

            // Lấy dữ liệu cần filter
            int lineCount = lineCountComboBox.SelectedIndex == 0 ? 1 :
                            lineCountComboBox.SelectedIndex == 1 ? 2 : 3;

            string[] lines = inputTextBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Chạy trong background
            filterWorker.RunWorkerAsync(new { Lines = lines, LineCount = lineCount });
        }

        private void FilterWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as dynamic;
            string[] lines = args.Lines;
            int lineCount = args.LineCount;

            // Thực hiện filter trong background
            var result = FilterData(lines, lineCount);
            e.Result = result;
        }

        private void FilterWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Khôi phục trạng thái UI
            filterButton.Enabled = true;
            filterButton.Text = "Lọc dữ liệu";
            Cursor = Cursors.Default;

            if (e.Error != null)
            {
                MessageBox.Show($"Lỗi khi lọc dữ liệu: {e.Error.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Cập nhật UI nhanh chóng
            if (e.Result is List<string> filteredData && filteredData.Count > 0)
            {
                resultTextBox.SuspendLayout();
                resultTextBox.Clear();
                resultTextBox.ForeColor = Color.Black;
                resultTextBox.Text = string.Join(Environment.NewLine, filteredData);
                ApplyAlternatingColors(resultTextBox);
                resultTextBox.ResumeLayout();
            }
            UpdateLineCount();
        }
        #endregion

        #region XỬ LÝ NÚT CLEAR VÀ EXPORT
        private void ClearButton_Click(object sender, EventArgs e)
        {
            inputTextBox.Clear();
            resultTextBox.Clear();
            SetPlaceholder(inputTextBox, "Paste dữ liệu vào đây...");
            SetPlaceholder(resultTextBox, "Kết quả sẽ hiển thị ở đây...");
            UpdateLineCount();//Cập nhật lại số dòng
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            // Kiểm tra có dữ liệu để xuất không
            if (string.IsNullOrWhiteSpace(resultTextBox.Text) ||
                resultTextBox.Text == "Kết quả sẽ hiển thị ở đây..." ||
                resultTextBox.ForeColor == Color.Gray)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Mở hộp thoại lưu file
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                saveDialog.DefaultExt = "txt";
                saveDialog.FileName = $"KetQua_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                saveDialog.Title = "Xuất dữ liệu ra file";

                // Thiết lập thư mục mặc định
                string defaultFolder = @"D:\Non_Documents";

                // Kiểm tra thư mục có tồn tại không, nếu không thì tạo
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
                        File.WriteAllText(saveDialog.FileName, resultTextBox.Text);
                        MessageBox.Show($"Xuất file thành công!\n{saveDialog.FileName}",
                            "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xuất file: {ex.Message}",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        #endregion

        #region PHƯƠNG THỨC LỌC DỮ LIỆU CHÍNH
        private List<string> FilterData(string[] lines, int lineCount)
        {
            var result = new List<string>();

            // Kiểm tra định dạng của dòng đầu tiên để xác định định dạng dữ liệu
            bool isTabFormat = false;
            if (lines.Length > 0)
            {
                // Nếu dòng đầu tiên chứa tab, sử dụng định dạng tab
                isTabFormat = lines[0].Contains("\t");
            }

            // Phân tách dữ liệu theo định dạng
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

            // Xử lý lọc dữ liệu
            foreach (var group in groups)
            {
                var key = group.Key;
                var values = group.ToList();

                if (values.Count == 1)
                {
                    // Nếu chỉ có một dòng, thêm trực tiếp vào kết quả
                    if (isTabFormat)
                        result.Add($"{key}\t{values[0][0]}\t{values[0][1]}");
                    else
                        result.Add($"{key}={values[0][0]},{values[0][1]}");
                    continue;
                }

                // Lọc các dòng khác nhau đáng kể
                var selectedIndices = SelectDifferentLines(values, lineCount);

                // Thêm kết quả theo định dạng tương ứng
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

        // Phương thức mới để chọn các dòng khác nhau nhất
        private List<int> SelectDifferentLines(List<int[]> values, int lineCount)
        {
            // Nếu chỉ yêu cầu 1 dòng, chọn dòng có khoảng cách trung bình lớn nhất
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

            // Với 2 hoặc 3 dòng, chọn các dòng có khoảng cách xa nhau nhất
            var selected = new List<int>();
            var remaining = Enumerable.Range(0, values.Count).ToList();

            // Bước 1: Chọn dòng đầu tiên (dòng có khoảng cách trung bình lớn nhất)
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

            // Ngưỡng khoảng cách tối thiểu để coi là "khác nhau"
            double minDistanceThreshold = 5.0; // Có thể điều chỉnh ngưỡng này

            // Bước 2-3: Chọn các dòng tiếp theo có khoảng cách xa nhất với các dòng đã chọn
            while (selected.Count < lineCount && remaining.Count > 0)
            {
                double maxMinDist = -1;
                int nextIndex = -1;

                foreach (var i in remaining)
                {
                    // Tính khoảng cách nhỏ nhất từ điểm này đến các điểm đã chọn
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

                    // Chọn điểm có khoảng cách nhỏ nhất lớn nhất (xa nhất với các điểm đã chọn)
                    if (minDist > maxMinDist)
                    {
                        maxMinDist = minDist;
                        nextIndex = i;
                    }
                }

                // Kiểm tra xem điểm tìm được có đủ khác biệt không
                if (nextIndex != -1 && maxMinDist >= minDistanceThreshold)
                {
                    selected.Add(nextIndex);
                    remaining.Remove(nextIndex);
                }
                else
                {
                    // Nếu không còn điểm nào đủ khác biệt, dừng lại
                    break;
                }
            }

            // Sắp xếp theo thứ tự ban đầu
            return selected.OrderBy(idx => idx).ToList();
        }
    }
    #endregion

// Phương pháp mở rộng để lấy dòng đầu tiên hiển thị
public static class RichTextBoxExtensions
{
    public static int GetFirstVisibleLineIndex(this RichTextBox rtb)
        {
         return rtb.GetLineFromCharIndex(rtb.GetCharIndexFromPosition(new Point(0, 0)));
        }
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