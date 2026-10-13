using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SimpleTextEncryptor
{
    public static class TextEncryptor
    {
        public const int SALT_SIZE = 16;
        public const int IV_SIZE = 16;
        public const int ITERATIONS = 100000;
        public const string ENC_EXTENSION = ".wt";

        public static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var rfc = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA256))
            {
                return rfc.GetBytes(32);
            }
        }

        public static string EncryptFile(string filePath, string password)
        {
            byte[] data = File.ReadAllBytes(filePath);
            string encFilePath = filePath + ENC_EXTENSION;
            if (filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                encFilePath = filePath.Substring(0, filePath.Length - 4) + ENC_EXTENSION;
            }

            EncryptToFile(data, password, encFilePath);
            File.Delete(filePath);
            return encFilePath;
        }

        public static void EncryptToFile(byte[] data, string password, string outputPath)
        {
            byte[] salt = new byte[SALT_SIZE];
            byte[] iv = new byte[IV_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
                rng.GetBytes(iv);
            }

            byte[] key = DeriveKey(password, salt);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] cipherText = encryptor.TransformFinalBlock(data, 0, data.Length);
                    using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        fs.Write(salt, 0, salt.Length);
                        fs.Write(iv, 0, iv.Length);
                        fs.Write(cipherText, 0, cipherText.Length);
                    }
                }
            }
        }

        public static byte[] DecryptToMemory(string filePath, string password)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length < SALT_SIZE + IV_SIZE)
            {
                throw new Exception("文件格式不正确或已损坏");
            }

            byte[] salt = new byte[SALT_SIZE];
            Array.Copy(fileBytes, 0, salt, 0, SALT_SIZE);

            byte[] iv = new byte[IV_SIZE];
            Array.Copy(fileBytes, SALT_SIZE, iv, 0, IV_SIZE);

            int cipherLen = fileBytes.Length - SALT_SIZE - IV_SIZE;
            byte[] cipherText = new byte[cipherLen];
            Array.Copy(fileBytes, SALT_SIZE + IV_SIZE, cipherText, 0, cipherLen);

            byte[] key = DeriveKey(password, salt);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    try
                    {
                        return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    }
                    catch
                    {
                        throw new Exception("密码错误或文件损坏");
                    }
                }
            }
        }
    }

    // 现代化圆角按钮控件
    public class ModernButton : Control
    {
        public int BorderRadius { get; set; }
        public Color NormalColor { get; set; }
        public Color HoverColor { get; set; }
        public Color PressedColor { get; set; }
        public Color BorderColor { get; set; }

        private bool isHovered = false;
        private bool isPressed = false;

        public ModernButton()
        {
            this.BorderRadius = 6;
            this.NormalColor = Color.FromArgb(0, 120, 212);
            this.HoverColor = Color.FromArgb(26, 134, 219);
            this.PressedColor = Color.FromArgb(0, 103, 184);
            this.BorderColor = Color.Transparent;

            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                          ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
            this.ForeColor = Color.White;
            this.Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold);
            this.Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; isPressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { isPressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { isPressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color bg = isPressed ? PressedColor : (isHovered ? HoverColor : NormalColor);
            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            using (GraphicsPath path = GetRoundedPath(rect, BorderRadius))
            {
                using (SolidBrush brush = new SolidBrush(bg))
                {
                    e.Graphics.FillPath(brush, path);
                }

                if (BorderColor != Color.Transparent)
                {
                    using (Pen pen = new Pen(BorderColor))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            TextRenderer.DrawText(e.Graphics, this.Text, this.Font, rect, this.ForeColor, 
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        private string targetFile;
        private TextBox passwordBox;
        private Panel pwdBoxContainer;
        private Label eyeBtn;
        private Label fileStatusLabel;
        private ModernButton actionBtn;
        private ModernButton associateBtn;
        private bool isPasswordMasked = true;
        private bool isPwdFocused = false;

        public MainForm(string file = null)
        {
            this.targetFile = file;
            BuildUI();
            ApplyWindows11Effects();
            UpdateFileState();
        }

        private void ApplyWindows11Effects()
        {
            try
            {
                // DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND = 2 (Win11 原生圆角)
                int cornerPreference = 2;
                DwmSetWindowAttribute(this.Handle, 33, ref cornerPreference, sizeof(int));

                // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (深色模式)
                int darkMode = 1;
                DwmSetWindowAttribute(this.Handle, 20, ref darkMode, sizeof(int));
            }
            catch { }
        }

        private void BuildUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(420, 440);
            this.BackColor = Color.FromArgb(24, 24, 24); // 深邃优雅暗黑
            this.Text = "极简文本加密助手";
            this.ShowIcon = true;
            this.ShowInTaskbar = true;
            this.AllowDrop = true;

            // 支持拖拽文件进入
            this.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            this.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    this.targetFile = files[0];
                    UpdateFileState();
                }
            };

            // 1. 自定义顶部标题栏 (高度 38px)
            Panel titleBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(this.Width, 38),
                BackColor = Color.FromArgb(24, 24, 24)
            };
            titleBar.MouseDown += OnDragWindow;

            Label titleLabel = new Label
            {
                Text = " 🔏 极简文本加密助手",
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(190, 190, 190),
                AutoSize = false,
                Location = new Point(10, 0),
                Size = new Size(200, 38),
                TextAlign = ContentAlignment.MiddleLeft
            };
            titleLabel.MouseDown += OnDragWindow;
            titleBar.Controls.Add(titleLabel);

            // 关闭按钮
            Label closeBtn = new Label
            {
                Text = "✕",
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(170, 170, 170),
                Size = new Size(44, 38),
                Location = new Point(this.Width - 44, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            closeBtn.MouseEnter += (s, e) => { closeBtn.BackColor = Color.FromArgb(232, 17, 35); closeBtn.ForeColor = Color.White; };
            closeBtn.MouseLeave += (s, e) => { closeBtn.BackColor = Color.Transparent; closeBtn.ForeColor = Color.FromArgb(170, 170, 170); };
            closeBtn.Click += (s, e) => Application.Exit();
            titleBar.Controls.Add(closeBtn);

            // 最小化按钮
            Label minBtn = new Label
            {
                Text = "—",
                Font = new Font("Microsoft YaHei", 8.5f),
                ForeColor = Color.FromArgb(170, 170, 170),
                Size = new Size(44, 38),
                Location = new Point(this.Width - 88, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            minBtn.MouseEnter += (s, e) => { minBtn.BackColor = Color.FromArgb(45, 45, 45); minBtn.ForeColor = Color.White; };
            minBtn.MouseLeave += (s, e) => { minBtn.BackColor = Color.Transparent; minBtn.ForeColor = Color.FromArgb(170, 170, 170); };
            minBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            titleBar.Controls.Add(minBtn);

            this.Controls.Add(titleBar);

            // 2. 主标题与副标题区
            Label headerTitle = new Label
            {
                Text = "安全加密 · 极简办公",
                Font = new Font("Microsoft YaHei", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 52),
                Size = new Size(this.Width, 32)
            };
            headerTitle.MouseDown += OnDragWindow;
            this.Controls.Add(headerTitle);

            Label headerSubtitle = new Label
            {
                Text = "AES-256-CBC 工业级加密标准",
                Font = new Font("Microsoft YaHei", 8.5f),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 84),
                Size = new Size(this.Width, 20)
            };
            headerSubtitle.MouseDown += OnDragWindow;
            this.Controls.Add(headerSubtitle);

            // 3. 文件状态卡片
            int contentWidth = 320;
            int startX = (this.Width - contentWidth) / 2;

            Panel fileCard = new Panel
            {
                Location = new Point(startX, 116),
                Size = new Size(contentWidth, 38),
                BackColor = Color.FromArgb(32, 32, 32),
                Cursor = Cursors.Hand
            };
            fileCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = ModernButton.GetRoundedPath(new Rectangle(0, 0, fileCard.Width - 1, fileCard.Height - 1), 6))
                {
                    using (Pen pen = new Pen(Color.FromArgb(48, 48, 48)))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            fileCard.Click += (s, e) => SelectFileManually();

            fileStatusLabel = new Label
            {
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(160, 160, 160),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            fileStatusLabel.Click += (s, e) => SelectFileManually();
            fileCard.Controls.Add(fileStatusLabel);
            this.Controls.Add(fileCard);

            // 4. 密码输入区域
            Label pwdLabel = new Label
            {
                Text = "访问密码",
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(startX + 2, 168),
                Size = new Size(contentWidth, 20)
            };
            this.Controls.Add(pwdLabel);

            // 现代化密码输入容器（带圆角和发光焦点）
            pwdBoxContainer = new Panel
            {
                Location = new Point(startX, 192),
                Size = new Size(contentWidth, 42),
                BackColor = Color.FromArgb(32, 32, 32)
            };
            pwdBoxContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color border = isPwdFocused ? Color.FromArgb(0, 120, 212) : Color.FromArgb(52, 52, 52);
                using (GraphicsPath path = ModernButton.GetRoundedPath(new Rectangle(0, 0, pwdBoxContainer.Width - 1, pwdBoxContainer.Height - 1), 6))
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(32, 32, 32)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen pen = new Pen(border, isPwdFocused ? 1.5f : 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            passwordBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 11f),
                PasswordChar = '●',
                Location = new Point(14, 11),
                Size = new Size(contentWidth - 52, 24)
            };
            passwordBox.GotFocus += (s, e) => { isPwdFocused = true; pwdBoxContainer.Invalidate(); };
            passwordBox.LostFocus += (s, e) => { isPwdFocused = false; pwdBoxContainer.Invalidate(); };
            passwordBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ExecuteCryptoAction();
                }
            };
            pwdBoxContainer.Controls.Add(passwordBox);

            // 密码显示/隐藏切换按钮
            eyeBtn = new Label
            {
                Text = "👁",
                Font = new Font("Segoe UI Emoji", 10.5f),
                ForeColor = Color.FromArgb(130, 130, 130),
                Location = new Point(contentWidth - 36, 6),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            eyeBtn.MouseEnter += (s, e) => eyeBtn.ForeColor = Color.FromArgb(0, 120, 212);
            eyeBtn.MouseLeave += (s, e) => eyeBtn.ForeColor = Color.FromArgb(130, 130, 130);
            eyeBtn.Click += (s, e) =>
            {
                isPasswordMasked = !isPasswordMasked;
                passwordBox.PasswordChar = isPasswordMasked ? '●' : '\0';
                eyeBtn.ForeColor = isPasswordMasked ? Color.FromArgb(130, 130, 130) : Color.FromArgb(0, 120, 212);
                passwordBox.Focus();
            };
            pwdBoxContainer.Controls.Add(eyeBtn);

            this.Controls.Add(pwdBoxContainer);

            // 5. 主操作按钮
            actionBtn = new ModernButton
            {
                Location = new Point(startX, 252),
                Size = new Size(contentWidth, 42),
                BorderRadius = 6,
                NormalColor = Color.FromArgb(0, 120, 212),
                HoverColor = Color.FromArgb(24, 138, 226),
                PressedColor = Color.FromArgb(0, 100, 180),
                Font = new Font("Microsoft YaHei", 10.5f, FontStyle.Bold)
            };
            actionBtn.Click += (s, e) => ExecuteCryptoAction();
            this.Controls.Add(actionBtn);

            // 6. 次级操作：关联右键菜单按钮
            associateBtn = new ModernButton
            {
                Location = new Point(startX, 304),
                Size = new Size(contentWidth, 34),
                BorderRadius = 6,
                NormalColor = Color.FromArgb(32, 32, 32),
                HoverColor = Color.FromArgb(44, 44, 44),
                PressedColor = Color.FromArgb(28, 28, 28),
                BorderColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(170, 170, 170),
                Font = new Font("Microsoft YaHei", 9f),
                Text = "🔗 关联 .wt 到系统右键 (免管理员)"
            };
            associateBtn.Click += (s, e) => RegisterContextMenu();
            this.Controls.Add(associateBtn);

            // 7. 底部安全说明
            Label footerLabel = new Label
            {
                Text = "🔒 内存解密防护 · 记事本关闭后自动回写并粉碎明文",
                Font = new Font("Microsoft YaHei", 8f),
                ForeColor = Color.FromArgb(88, 88, 88),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 395),
                Size = new Size(this.Width, 24)
            };
            this.Controls.Add(footerLabel);

            // 8. 窗口细致深色外边框
            this.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(46, 46, 46), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            this.Load += (s, e) => passwordBox.Focus();
        }

        private void OnDragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void UpdateFileState()
        {
            if (!string.IsNullOrEmpty(targetFile) && File.Exists(targetFile))
            {
                string ext = Path.GetExtension(targetFile).ToLower();
                string fileName = Path.GetFileName(targetFile);

                if (ext == TextEncryptor.ENC_EXTENSION)
                {
                    fileStatusLabel.Text = "📂 解密文件: " + fileName;
                    fileStatusLabel.ForeColor = Color.FromArgb(97, 175, 239); // 亮蓝
                    actionBtn.Text = "🔓  解密并查看";
                    actionBtn.NormalColor = Color.FromArgb(0, 120, 212);
                }
                else
                {
                    fileStatusLabel.Text = "📄 待加密: " + fileName;
                    fileStatusLabel.ForeColor = Color.FromArgb(152, 195, 121); // 浅绿
                    actionBtn.Text = "🔒  立即加密";
                    actionBtn.NormalColor = Color.FromArgb(39, 134, 82);
                    actionBtn.HoverColor = Color.FromArgb(46, 160, 98);
                    actionBtn.PressedColor = Color.FromArgb(30, 110, 65);
                }
            }
            else
            {
                fileStatusLabel.Text = "📁 点击或拖拽文件到此处选择";
                fileStatusLabel.ForeColor = Color.FromArgb(140, 140, 140);
                actionBtn.Text = "🔒  选择文件并加密";
                actionBtn.NormalColor = Color.FromArgb(0, 120, 212);
                actionBtn.HoverColor = Color.FromArgb(24, 138, 226);
                actionBtn.PressedColor = Color.FromArgb(0, 100, 180);
            }
            actionBtn.Invalidate();
        }

        private void SelectFileManually()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "选择要加密或解密的文件";
                ofd.Filter = "支持的文件 (*.txt;*.wt)|*.txt;*.wt|文本文件 (*.txt)|*.txt|加密文件 (*.wt)|*.wt|所有文件 (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    this.targetFile = ofd.FileName;
                    UpdateFileState();
                    passwordBox.Focus();
                }
            }
        }

        private void RegisterContextMenu()
        {
            string exePath = Application.ExecutablePath;
            try
            {
                RegistryKey baseKey = Registry.CurrentUser;

                // 1. 关联扩展名 .wt
                using (RegistryKey extKey = baseKey.CreateSubKey(@"Software\Classes\.wt"))
                {
                    extKey.SetValue("", "SimpleTextEncryptor");
                }

                // 2. 注册打开命令
                using (RegistryKey cmdKey = baseKey.CreateSubKey(@"Software\Classes\SimpleTextEncryptor\shell\open\command"))
                {
                    cmdKey.SetValue("", "\"" + exePath + "\" \"%1\"");
                }

                // 3. 注册关联图标
                using (RegistryKey iconKey = baseKey.CreateSubKey(@"Software\Classes\SimpleTextEncryptor\DefaultIcon"))
                {
                    iconKey.SetValue("", "\"" + exePath + "\",0");
                }

                // 4. 刷新 Shell 缓存
                SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
                MessageBox.Show("右键关联成功！\n现在双击 .wt 加密文件即可直接调用本程序打开。", "关联成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExecuteCryptoAction()
        {
            string password = passwordBox.Text;
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入访问密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                passwordBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(targetFile))
            {
                SelectFileManually();
                if (string.IsNullOrEmpty(targetFile)) return;
            }

            if (!File.Exists(targetFile))
            {
                MessageBox.Show("目标文件不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (targetFile.EndsWith(TextEncryptor.ENC_EXTENSION, StringComparison.OrdinalIgnoreCase))
                {
                    // 1. 解密到内存
                    byte[] originalData = TextEncryptor.DecryptToMemory(targetFile, password);

                    // 2. 写入临时明文文件
                    string tmpPath = Path.Combine(Path.GetTempPath(), "secure_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt");
                    File.WriteAllBytes(tmpPath, originalData);

                    // 隐藏当前窗口
                    this.Hide();

                    // 3. 调起记事本并等待编辑关闭
                    using (Process proc = Process.Start("notepad.exe", "\"" + tmpPath + "\""))
                    {
                        if (proc != null)
                        {
                            proc.WaitForExit();
                        }
                    }

                    // 4. 读取记事本保存后的新内容
                    byte[] newData = File.ReadAllBytes(tmpPath);
                    if (!ByteArrayEqual(newData, originalData))
                    {
                        TextEncryptor.EncryptToFile(newData, password, targetFile);
                    }

                    // 5. 安全抹除临时文件
                    try
                    {
                        if (File.Exists(tmpPath))
                        {
                            // 覆写 0 后删除，防止数据恢复
                            File.WriteAllBytes(tmpPath, new byte[newData.Length]);
                            File.Delete(tmpPath);
                        }
                    }
                    catch { }

                    Application.Exit();
                }
                else
                {
                    // 加密逻辑
                    string encPath = TextEncryptor.EncryptFile(targetFile, password);
                    MessageBox.Show("已成功加密为：\n" + Path.GetFileName(encPath), "加密完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("密码不正确或文件已损坏！", "认证失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                passwordBox.SelectAll();
                passwordBox.Focus();
            }
        }

        private static bool ByteArrayEqual(byte[] b1, byte[] b2)
        {
            if (b1 == b2) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string target = (args != null && args.Length > 0) ? args[0] : null;
            Application.Run(new MainForm(target));
        }
    }
}
