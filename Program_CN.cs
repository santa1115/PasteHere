/*
 * Project: PasteHere (Native Windows Implementation) - Chinese Version
 * Author: Hua & Antigravity
 * License: MIT
 * Description: A lightweight (13KB), native tool to paste clipboard content (Images, Text, Files) 
 *              directly into the file system. Zero dependencies.
 * repo: https://github.com/santa1115/PasteHere
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PasteHere
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                // Paste Mode
                string targetDir = args[0].Replace("\"", "");
                if (Directory.Exists(targetDir))
                {
                    SaveClipboardContent(targetDir);
                }
                else
                {
                    MessageBox.Show("目标文件夹不存在:\n" + targetDir, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Install/Uninstall Mode
                DialogResult res = MessageBox.Show(
                    "本工具可以直接将剪贴板中的 [图片] 或 [文件链接] 粘贴为文件。\n\n" +
                    "是否将 '粘贴文件' 添加到右键菜单？\n\n" +
                    "[是 Yes] - 安装\n[否 No] - 卸载\n[取消 Cancel] - 退出",
                    "Paste Here (安装程序 V3.3)",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    RegisterContextMenu();
                }
                else if (res == DialogResult.No)
                {
                    UnregisterContextMenu();
                }
            }
        }

        static void RegisterContextMenu()
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string command = "\"" + exePath + "\" \"%V\"";
                string menuName = "粘贴文件";

                string[] keys = {
                    @"Software\Classes\Directory\shell\PasteHere",
                    @"Software\Classes\Directory\Background\shell\PasteHere"
                };

                foreach (string keyPath in keys)
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
                    {
                        key.SetValue("", menuName);
                        key.SetValue("Icon", "shell32.dll,260");

                        using (RegistryKey cmdKey = key.CreateSubKey("command"))
                        {
                            cmdKey.SetValue("", command);
                        }
                    }
                }
                MessageBox.Show("安装成功，右键-“粘贴文件”即可使用。\n\n作者：华", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("安装失败:\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void UnregisterContextMenu()
        {
            try
            {
                string[] keys = {
                    @"Software\Classes\Directory\shell\PasteHere",
                    @"Software\Classes\Directory\Background\shell\PasteHere"
                };

                foreach (string keyPath in keys)
                {
                    Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
                }
                MessageBox.Show("卸载成功。右键菜单已移除。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("卸载失败:\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void SaveClipboardContent(string targetDir)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            bool actionTaken = false;

            try
            {
                IDataObject data = Clipboard.GetDataObject();

                // 1. Files
                if (Clipboard.ContainsFileDropList())
                {
                    StringCollection files = Clipboard.GetFileDropList();
                    foreach (string srcPath in files)
                    {
                        if (File.Exists(srcPath))
                        {
                            string fileName = Path.GetFileName(srcPath);
                            string finalPath = Path.Combine(targetDir, fileName);
                            
                            if (File.Exists(finalPath))
                            {
                                string baseName = Path.GetFileNameWithoutExtension(fileName);
                                string ext = Path.GetExtension(fileName);
                                finalPath = Path.Combine(targetDir, String.Format("{0}_{1}{2}", baseName, timestamp, ext));
                            }
                            File.Copy(srcPath, finalPath);
                            actionTaken = true;
                        }
                    }
                    if (actionTaken) return;
                }

                // 2. Text (URL Check or Plain Text)
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText().Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // File Download Check
                        if (text.StartsWith("http", StringComparison.OrdinalIgnoreCase) && HasFileExtension(text))
                        {
                            string fileName = Path.GetFileName(text);
                            // Clean up query params
                            if (fileName.Contains("?")) fileName = fileName.Substring(0, fileName.IndexOf('?'));
                            
                            if (string.IsNullOrEmpty(Path.GetExtension(fileName))) fileName = "Download_" + timestamp + ".file";

                            string savePath = Path.Combine(targetDir, fileName);
                            
                            // Download with Progress UI
                            if (DownloadFileWithUI(text, savePath))
                            {
                                actionTaken = true;
                                return;
                            }
                        }

                        // Save as Text
                        string txtPath = Path.Combine(targetDir, String.Format("Paste_{0}.txt", timestamp));
                        try
                        {
                            File.WriteAllText(txtPath, text);
                            actionTaken = true;
                        } catch { }
                    }
                }

                // 3. HTML / Web Images
                int imagesProcessed = 0;
                if (data.GetDataPresent("HTML Format"))
                {
                    string htmlContent = (string)data.GetData("HTML Format");
                    var images = ExtractImages(htmlContent);

                    int idx = 0;
                    foreach (var imgInfo in images)
                    {
                        idx++;
                        string suffix = images.Count > 1 ? "_" + idx : "";
                        string savePath = Path.Combine(targetDir, String.Format("Image_{0}{1}.png", timestamp, suffix));
                        bool success = false;

                        if (imgInfo.Type == "url")
                            success = DownloadFileWithUI(imgInfo.Data, savePath); 
                        else if (imgInfo.Type == "base64")
                            success = SaveBase64Image(imgInfo.Data, savePath);

                        if (success)
                        {
                            imagesProcessed++;
                            actionTaken = true;
                        }
                    }
                }

                // 4. Bitmap Fallback
                if (imagesProcessed == 0 && Clipboard.ContainsImage())
                {
                    Image img = Clipboard.GetImage();
                    if (img != null)
                    {
                        string imgPath = Path.Combine(targetDir, String.Format("Image_{0}.png", timestamp));
                        img.Save(imgPath, ImageFormat.Png);
                        actionTaken = true;
                    }
                }

                if (!actionTaken)
                {
                    MessageBox.Show("剪贴板为空或内容不支持。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static bool HasFileExtension(string url)
        {
            string[] exts = { ".zip", ".exe", ".pdf", ".jpg", ".png", ".mp4", ".mp3", ".docx" };
            foreach(var e in exts) {
                if (url.IndexOf(e, StringComparison.OrdinalIgnoreCase) > 0) return true;
            }
            return false;
        }

        static bool DownloadFileWithUI(string url, string savePath)
        {
            ProgressForm pf = new ProgressForm(url, savePath);
            Application.Run(pf);
            return pf.Success;
        }

        static List<ImageInfo> ExtractImages(string html)
        {
            var list = new List<ImageInfo>();
            if (string.IsNullOrEmpty(html)) return list;

            html = System.Net.WebUtility.HtmlDecode(html);

            Regex regexBase64 = new Regex(@"src=[""'](data:image/[^;]+;base64,[^""']+)[""']", RegexOptions.IgnoreCase);
            foreach (Match m in regexBase64.Matches(html))
            {
                list.Add(new ImageInfo { Type = "base64", Data = m.Groups[1].Value });
            }

            Regex regexUrl = new Regex(@"src=[""'](http[^""']+)[""']", RegexOptions.IgnoreCase);
            foreach (Match m in regexUrl.Matches(html))
            {
                string url = m.Groups[1].Value;
                url = GetHighResUrl(url);
                list.Add(new ImageInfo { Type = "url", Data = url });
            }

            return list;
        }

        static string GetHighResUrl(string url)
        {
            if (url.Contains("wikipedia.org") && url.Contains("/thumb/"))
            {
                Regex r = new Regex(@"(.+)/thumb/(.+)/[^/]+$");
                Match m = r.Match(url);
                if (m.Success)
                {
                    return m.Groups[1].Value + "/" + m.Groups[2].Value;
                }
            }
            return url;
        }

        static bool SaveBase64Image(string dataUri, string savePath)
        {
            try
            {
                int commaIndex = dataUri.IndexOf(',');
                if (commaIndex > 0)
                {
                    string b64 = dataUri.Substring(commaIndex + 1);
                    byte[] bytes = Convert.FromBase64String(b64);
                    File.WriteAllBytes(savePath, bytes);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        class ImageInfo
        {
            public string Type;
            public string Data;
        }
    }

    // --- Progress UI (Chinese) ---
    public class ProgressForm : Form
    {
        public bool Success = false;
        private WebClient _client;
        private string _url;
        private string _savePath;
        private ProgressBar _bar;
        private Label _lblStatus;

        public ProgressForm(string url, string savePath)
        {
            _url = url;
            _savePath = savePath;
            InitializeComponent();
            this.Load += OnLoad;
        }

        private void InitializeComponent()
        {
            this.Size = new Size(400, 120);
            this.Text = "下载中...";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            _lblStatus = new Label();
            _lblStatus.Location = new Point(10, 10);
            _lblStatus.Size = new Size(360, 20);
            _lblStatus.Text = "正在连接...";

            _bar = new ProgressBar();
            _bar.Location = new Point(10, 40);
            _bar.Size = new Size(360, 20);

            this.Controls.Add(_lblStatus);
            this.Controls.Add(_bar);
        }

        private void OnLoad(object sender, EventArgs e)
        {
            StartDownload();
        }

        private void StartDownload()
        {
            try {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            } catch { }

            _client = new WebClient();
            _client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            _client.DownloadProgressChanged += (s, ev) =>
            {
                _bar.Value = ev.ProgressPercentage;
                _lblStatus.Text = String.Format("下载进度: {0}% ({1} KB)", ev.ProgressPercentage, ev.BytesReceived / 1024);
            };
            _client.DownloadFileCompleted += (s, ev) =>
            {
                if (ev.Error != null)
                {
                    MessageBox.Show("下载错误: " + ev.Error.Message);
                    Success = false;
                }
                else
                {
                    Success = true;
                }
                this.Close();
            };

            try
            {
                _client.DownloadFileAsync(new Uri(_url), _savePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化错误: " + ex.Message);
                this.Close();
            }
        }
    }
}
