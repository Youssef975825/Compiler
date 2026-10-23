using System.Runtime.InteropServices;
using System.Text;
using Compiler.Lexing;

namespace Compiler;

public partial class Form1 : Form
{
    private readonly Scanner _scanner = new();
    private List<Token> _tokens = new();
    private string? _currentFile;

    private static readonly Color DefaultColor = Color.FromArgb(40, 40, 40);
    private static readonly Color KeywordColor = Color.FromArgb(0, 92, 197);
    private static readonly Color NumberColor = Color.FromArgb(130, 70, 210);
    private static readonly Color StringColor = Color.FromArgb(200, 100, 20);
    private static readonly Color CommentColor = Color.FromArgb(40, 140, 70);
    private static readonly Color ErrorColor = Color.FromArgb(200, 30, 30);
    private readonly Scanner _liveScanner = new() { KeepComments = true };
    private readonly System.Windows.Forms.Timer _liveTimer = new() { Interval = 400 };
    private readonly ToolTip _tip = new();
    private Token? _hoverToken;
    private List<Token> _allTokens = new();
    private bool _highlighting;

    private const int EM_GETSCROLLPOS = 0x0400 + 221;
    private const int EM_SETSCROLLPOS = 0x0400 + 222;

    [DllImport("user32.dll", EntryPoint = "SendMessage")]
    private static extern int SendMessageScroll(IntPtr hWnd, int msg, int wParam, ref Point lParam);

    private Font _normalFont = null!;
    private Font _italicFont = null!;
    private Font _underlineFont = null!;


    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    private const int WM_SETREDRAW = 0x000B;
    

    public Form1()
    {
        InitializeComponent();
        SetupGrids();
        SetupEditor();
        SetupHover();
        _scanner.KeepComments = true;

        _liveTimer.Tick += (_, _) =>
        {
            _liveTimer.Stop();
            LiveHighlight();
        };

        rtbCode.TextChanged += (_, _) =>
        {
            if (_highlighting)
                return;

            _liveTimer.Stop();
            _liveTimer.Start();
        };
    }

    private void SetupGrids()
    {
        foreach (DataGridView grid in new[] { dgvTokens, dgvErrors })
        {
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(238, 240, 245);
            grid.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Consolas", 10F);
        }


        dgvTokens.Columns.Clear();
        dgvTokens.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Lexeme", DataPropertyName = "Lexeme", FillWeight = 40
        });
        dgvTokens.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Type", DataPropertyName = "Type", FillWeight = 35
        });
        dgvTokens.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Line", DataPropertyName = "Line", FillWeight = 12
        });
        dgvTokens.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Col", DataPropertyName = "Column", FillWeight = 13
        });


        dgvErrors.Columns.Clear();
        dgvErrors.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Line", DataPropertyName = "Line", FillWeight = 10
        });
        dgvErrors.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Col", DataPropertyName = "Column", FillWeight = 10
        });
        dgvErrors.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Message", DataPropertyName = "Message", FillWeight = 80
        });


        dgvErrors.DefaultCellStyle.BackColor = Color.FromArgb(255, 226, 226);
        dgvErrors.DefaultCellStyle.ForeColor = Color.FromArgb(160, 20, 20);
        dgvErrors.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 180, 180);
        dgvErrors.DefaultCellStyle.SelectionForeColor = Color.FromArgb(120, 0, 0);


        dgvErrors.CellClick += dgvErrors_CellClick;
    }

    // open button
    private void button1_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Rynix files (*.rx;*.txt)|*.rx;*.txt|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        rtbCode.Text = File.ReadAllText(dialog.FileName);
        _currentFile = dialog.FileName;
    }

    // save button
    private void button2_Click(object sender, EventArgs e)
    {
        if (_currentFile == null)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Rynix files (*.rx)|*.rx|Text files (*.txt)|*.txt"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            _currentFile = dialog.FileName;
        }

        File.WriteAllText(_currentFile, rtbCode.Text);
    }

    // scan button
    private void button3_Click(object sender, EventArgs e)
    {
        _liveTimer.Stop();
        RunScan();
    }

    private void RunScan()
    {
        List<Token> all = _scanner.Scan(rtbCode.Text);

        _tokens = all.Where(t => t.Type != TokenType.Comment).ToList();

        ShowTokens();
        ShowErrors();
        ShowStats();

        LiveHighlight();
    }

    // Clear button
    private void button4_Click(object sender, EventArgs e)
    {
        rtbCode.Clear();
        _tokens = new List<Token>();
        _currentFile = null;

        dgvTokens.DataSource = null;
        dgvErrors.DataSource = null;
        lblStats.Text = "Tokens: 0  |  Keywords: 0  |  Identifiers: 0  |  Numbers: 0  |  Errors: 0";
    }

    // Export button
    private void button5_Click(object sender, EventArgs e)
    {
        if (_tokens.Count == 0)
        {
            MessageBox.Show("Scan the code first.", "Export CSV");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = "tokens.csv"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("Lexeme,Type,Line,Column");

        foreach (Token t in _tokens)
            sb.AppendLine($"{Csv(t.Lexeme)},{t.Type},{t.Line},{t.Column}");

        File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
    }

    private static string Csv(string text)
    {
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private void ShowTokens()
    {
        var shown = _tokens.Where(t => t.Type != TokenType.EOF).ToList();
        dgvTokens.DataSource = null;
        dgvTokens.DataSource = shown;
    }

    private void ShowErrors()
    {
        dgvErrors.DataSource = null;
        dgvErrors.DataSource = _scanner.Errors.ToList();
    }

    private void ShowStats()
    {
        int tokens = _tokens.Count(t => t.Type != TokenType.EOF);
        int keywords = _tokens.Count(t => t.Type.IsKeyword());
        int identifiers = _tokens.Count(t => t.Type == TokenType.Identifier);
        int numbers = _tokens.Count(t => t.Type == TokenType.Integer
                                         || t.Type == TokenType.Float);
        int errors = _scanner.Errors.Count;

        lblStats.Text =
            $"Tokens: {tokens}  |  Keywords: {keywords}  |  " +
            $"Identifiers: {identifiers}  |  Numbers: {numbers}  |  Errors: {errors}";
    }

    private void dgvErrors_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        if (dgvErrors.Rows[e.RowIndex].DataBoundItem is not Error error)
            return;

        rtbCode.Focus();
        rtbCode.Select(error.Position, 1);
        rtbCode.ScrollToCaret();
    }

    private void SetupEditor()
    {
        rtbCode.Font = new Font("Consolas", 11F);
        rtbCode.BackColor = Color.White;
        rtbCode.ForeColor = DefaultColor;
        rtbCode.BorderStyle = BorderStyle.None;
        rtbCode.WordWrap = false;
        rtbCode.AcceptsTab = true;
        rtbCode.DetectUrls = false;

        _normalFont = rtbCode.Font;
        _italicFont = new Font(_normalFont, FontStyle.Italic);
        _underlineFont = new Font(_normalFont, FontStyle.Underline);


        pnlLines.BackColor = Color.FromArgb(246, 247, 250);
        typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)!
            .SetValue(pnlLines, true);

        pnlLines.Paint += pnlLines_Paint;
        rtbCode.VScroll += (_, _) => pnlLines.Invalidate();
        rtbCode.TextChanged += (_, _) => pnlLines.Invalidate();
        rtbCode.Resize += (_, _) => pnlLines.Invalidate();
    }

    private void pnlLines_Paint(object? sender, PaintEventArgs e)
    {
        int total = rtbCode.GetLineFromCharIndex(rtbCode.TextLength) + 1;
        int first = rtbCode.GetLineFromCharIndex(
            rtbCode.GetCharIndexFromPosition(new Point(0, 0)));

        for (int i = first; i < total; i++)
        {
            int charIndex = rtbCode.GetFirstCharIndexFromLine(i);
            if (charIndex < 0)
                break;

            Point p = rtbCode.GetPositionFromCharIndex(charIndex);
            if (p.Y > pnlLines.Height)
                break;

            string number = (i + 1).ToString();
            Size size = TextRenderer.MeasureText(number, rtbCode.Font);

            TextRenderer.DrawText(
                e.Graphics, number, rtbCode.Font,
                new Point(pnlLines.Width - size.Width - 8, p.Y),
                Color.Gray);
        }
    }

    private (Color color, Font font)? StyleFor(TokenType type)
    {
        return type switch
        {
            _ when type.IsKeyword() => (KeywordColor, _normalFont),
            TokenType.Integer or TokenType.Float => (NumberColor, _normalFont),
            TokenType.String or TokenType.Char => (StringColor, _normalFont),
            TokenType.Comment => (CommentColor, _italicFont),
            TokenType.Error => (ErrorColor, _underlineFont),
            _ => null
        };
    }

    private void Highlight(List<Token> tokens)
    {
        _highlighting = true;

        int caretStart = rtbCode.SelectionStart;
        int caretLength = rtbCode.SelectionLength;

        Point scroll = Point.Empty;
        SendMessageScroll(rtbCode.Handle, EM_GETSCROLLPOS, 0, ref scroll);

        SendMessage(rtbCode.Handle, WM_SETREDRAW, 0, 0);
        try
        {
            rtbCode.SelectAll();
            rtbCode.SelectionColor = DefaultColor;
            rtbCode.SelectionFont = _normalFont;

            foreach (Token t in tokens)
            {
                var style = StyleFor(t.Type);
                if (style == null || t.Lexeme.Length == 0)
                    continue;

                rtbCode.Select(t.Position, t.Lexeme.Length);
                rtbCode.SelectionColor = style.Value.color;
                rtbCode.SelectionFont = style.Value.font;
            }

            rtbCode.Select(caretStart, caretLength);
            if (caretLength == 0)
            {
                rtbCode.SelectionColor = DefaultColor;
                rtbCode.SelectionFont = _normalFont;
            }
        }
        finally
        {
            SendMessageScroll(rtbCode.Handle, EM_SETSCROLLPOS, 0, ref scroll);
            SendMessage(rtbCode.Handle, WM_SETREDRAW, 1, 0);
            rtbCode.Invalidate();
            _highlighting = false;
        }
    }

    private void SetupHover()
    {
        rtbCode.MouseMove += rtbCode_MouseMove;
        rtbCode.MouseLeave += (_, _) =>
        {
            _hoverToken = null;
            _tip.Hide(rtbCode);
        };
    }

    private Token? FindToken(int index)
    {
        int lo = 0, hi = _allTokens.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            Token t = _allTokens[mid];

            if (index < t.Position)
                hi = mid - 1;
            else if (index >= t.Position + t.Lexeme.Length)
                lo = mid + 1;
            else
                return t;
        }

        return null;
    }

    private void rtbCode_MouseMove(object? sender, MouseEventArgs e)
    {
        Token? token = null;

        int index = rtbCode.GetCharIndexFromPosition(e.Location);

        if (index < rtbCode.TextLength)
        {
            Point left = rtbCode.GetPositionFromCharIndex(index);
            int charWidth = TextRenderer.MeasureText("WW", rtbCode.Font).Width
                            - TextRenderer.MeasureText("W", rtbCode.Font).Width;

            bool inside = e.X >= left.X && e.X <= left.X + charWidth
                                        && e.Y >= left.Y && e.Y <= left.Y + rtbCode.Font.Height;

            if (inside)
                token = FindToken(index);
        }

        if (token == _hoverToken)
            return;

        _hoverToken = token;

        if (token == null)
        {
            _tip.Hide(rtbCode);
            return;
        }

        _tip.Show(BuildTip(token), rtbCode, e.X + 14, e.Y + 22, 10000);
    }

    private string BuildTip(Token t)
    {
        var sb = new StringBuilder();

        int end = t.Position + Math.Max(1, t.Lexeme.Length);
        var errors = _liveScanner.Errors
            .Where(er => er.Position >= t.Position && er.Position < end)
            .ToList();

        foreach (Error er in errors)
            sb.AppendLine($"ERROR: {er.Message}");

        sb.AppendLine($"{t.Type}: {t.Lexeme}");
        sb.Append($"Line {t.Line}, Col {t.Column}");

        if (t.Value != null && t.Value.ToString() != t.Lexeme
                            && t.Type != TokenType.Error)
        {
            string shown = t.Value.ToString()!
                .Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r");

            sb.AppendLine();
            sb.Append($"Value: {shown}  ({t.Value.GetType().Name})");
        }

        return sb.ToString();
    }


    private void LiveHighlight()
    {
        _allTokens = _liveScanner.Scan(rtbCode.Text);
        Highlight(_allTokens);
    }
}