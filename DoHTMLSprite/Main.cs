using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace DoHTMLSprite
{
    public partial class Main : Form
    {
        string sourceFolder = string.Empty, destinationFolder = string.Empty;
        List<SpriteImage> imagesToProcess = new List<SpriteImage>();
        int MaxWidth = 0, MaxHeight = 0;

        public Main()
        {
            InitializeComponent();

            if (!String.IsNullOrEmpty(DoHTMLSprite.Properties.Settings.Default.lastdir))
                folderBrowser.SelectedPath = DoHTMLSprite.Properties.Settings.Default.lastdir;
        }

        private void btnSourceFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DoHTMLSprite.Properties.Settings.Default.lastdir = sourceFolder = folderBrowser.SelectedPath;
                DoHTMLSprite.Properties.Settings.Default.Save();
                btnTargetFolder.Enabled = btnCreate.Enabled = true;
                lblSourceFolder.Text = sourceFolder;
                imagesToProcess = GetFiles();
            }
        }

        private void btnTargetFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                destinationFolder = folderBrowser.SelectedPath;
                lblDestinationFolder.Text = destinationFolder;
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(destinationFolder))
                destinationFolder = lblDestinationFolder.Text = sourceFolder;
            
            DoSpriteImg();
        }


        /// <summary>
        /// Returns a list with the image files available to be added to the sprite
        /// </summary>
        /// <returns></returns>
        private List<SpriteImage> GetFiles()
        {
            DirectoryInfo dir = new DirectoryInfo(sourceFolder);
            
            if (!dir.Exists)
                lblStatus.Text = "Source directory not found!";
            
            List<FileInfo> files = dir.GetFiles("*.*", SearchOption.TopDirectoryOnly).ToList();
            List<SpriteImage> spriteImages = new List<SpriteImage>();

            files = (from f in files
                     where
                        f.Name.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                        f.Name.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
                        f.Name.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                        f.Name.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase) ||
                        f.Name.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase)
                     orderby f.Name
                     select f).ToList();

            lblStatus.Text = (files.Count == 0) ? "No image file was found!" : files.Count.ToString()+" images found.";

            foreach (FileInfo f in files)
            {
                SpriteImage simage = new SpriteImage();
                simage.imgInfo = f;
                simage.sImage = Image.FromFile(f.FullName);
                spriteImages.Add(simage);

                if (simage.sImage.Size.Width > MaxWidth)
                    MaxWidth = simage.sImage.Size.Width;

                MaxHeight += simage.sImage.Size.Height+5;
            }

            return spriteImages;
        }

        private void DoSpriteImg()
        {
            int lastY = 0;
            StringBuilder sbCSS = new StringBuilder();
            StringBuilder sbHTML = new StringBuilder();

            Bitmap sprite = new Bitmap(MaxWidth, MaxHeight);
            Graphics g = Graphics.FromImage(sprite);
            //g.Clear(Color.Red);

            for (int i = 0; i < imagesToProcess.Count(); i++)
            { 
                g.DrawImage(imagesToProcess[i].sImage, 0, lastY, imagesToProcess[i].sImage.Size.Width, imagesToProcess[i].sImage.Size.Height);
                sbCSS.AppendLine(string.Format(".csssprite{0} {{ background-position: 0 {1}px; width: {2}px; height: {3}px; margin:5px 0 }}", i, (lastY > 0) ? (-lastY) : 0, imagesToProcess[i].sImage.Size.Width, imagesToProcess[i].sImage.Size.Height));
                lastY += imagesToProcess[i].sImage.Size.Height+5;

                // create sample html page
                sbHTML.AppendLine("<div class=\"spriteImage csssprite" + i.ToString() + "\"></div>");
            }

            
            // write image file
            string fileName = Guid.NewGuid().ToString();
            sprite.Save(destinationFolder + @"\" + fileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
            sbCSS.Insert(0, ".spriteImage { background-image: url(" + fileName + ".png) } ");
            FileStream fsCSS = new FileStream(destinationFolder + @"\" + fileName +".css", FileMode.Create);
            StreamWriter swCSS = new StreamWriter(fsCSS);
            swCSS.Write(sbCSS.ToString());
            swCSS.Flush();
            fsCSS.Close();
            // write HTML file
            sbHTML.Insert(0, "<head><LINK REL=StyleSheet HREF=\"" + fileName + ".css\" TYPE=\"text/css\"></head><body>");
            sbHTML.Append("</body>");
            FileStream fsHTML = new FileStream(destinationFolder + @"\" + fileName + ".html", FileMode.Create);
            StreamWriter swHTML = new StreamWriter(fsHTML);
            swHTML.Write(sbHTML.ToString());
            swHTML.Flush();
            fsHTML.Close();
            MessageBox.Show("Sprite image, css file and demo html done.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

public struct SpriteImage
{
    public Image sImage;
    public FileInfo imgInfo;
}
