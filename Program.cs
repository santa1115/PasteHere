/*
 * Project: PasteHere (Native Windows Implementation)
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
                    MessageBox.Show("Target directory does not exist:\n" + targetDir, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Install/Uninstall Mode
                DialogResult res = MessageBox.Show(
                    "This tool allows you to paste images and file URLs directly into files.\n\n" +
                    "Do you want to add 'Paste Here' to your context menu?\n\n" +
                    "[Yes] - Install\n[No] - Uninstall\n[Cancel] - Exit",
                    "Paste Here (Installer V3.3)",
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
                string menuName = "Paste Here";

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
                MessageBox.Show("Success! 'Paste Here' has been added to the right-click menu.\n\nAuthor: Hua", "Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Installation Failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Uninstalled. 'Paste Here' has been removed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Uninstallation Failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                             // Clean up query params if simple extraction fails to look like file
                            if (fileName.Contains("?")) fileName = fileName.Substring(0, fileName.IndexOf('?'));
                            
                            if (string.IsNullOrEmpty(Path.GetExtension(fileName))) fileName = "Download_" + timestamp + ".file";

                            string savePath = Path.Combine(targetDir, fileName);
                            
                            // Download with Progress UI
                            if (DownloadFileWithUI(text, savePath))
                            {
                                actionTaken = true;
                                return; // Don't save text if we downloaded the file
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
                            success = DownloadFileWithUI(imgInfo.Data, savePath); // Use UI even for images
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
                    MessageBox.Show("Clipboard is empty or contains unsupported content.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving content:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static bool HasFileExtension(string url)
        {
            // Simple heuristic
            string[] exts = { ".zip", ".exe", ".pdf", ".jpg", ".png", ".mp4", ".mp3", ".docx" };
            foreach(var e in exts) {
                if (url.IndexOf(e, StringComparison.OrdinalIgnoreCase) > 0) return true;
            }
            return false;
        }

        // --- Downloading Logic with UI ---

        static bool DownloadFileWithUI(string url, string savePath)
        {
            // If it's a tiny image, maybe skip UI? No, let's keep it consistent for smoothness.
            ProgressForm pf = new ProgressForm(url, savePath);
            Application.Run(pf); // Blocks until form closes
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

    // --- Progress UI ---
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
            this.Text = "Downloading...";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            _lblStatus = new Label();
            _lblStatus.Location = new Point(10, 10);
            _lblStatus.Size = new Size(360, 20);
            _lblStatus.Text = "Connecting...";

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
            // Fix: Force TLS 1.2 for modern sites (GitHub etc.)
            try {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            } catch { /* If running on very old .NET that doesn't support it, ignore */ }

            _client = new WebClient();
            _client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            _client.DownloadProgressChanged += (s, ev) =>
            {
                _bar.Value = ev.ProgressPercentage;
                _lblStatus.Text = String.Format("Downloading: {0}% ({1} KB)", ev.ProgressPercentage, ev.BytesReceived / 1024);
            };
            _client.DownloadFileCompleted += (s, ev) =>
            {
                if (ev.Error != null)
                {
                    MessageBox.Show("Download Error: " + ev.Error.Message);
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
                MessageBox.Show("Init Error: " + ex.Message);
                this.Close();
            }
        }
    }
}
