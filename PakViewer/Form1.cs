
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PakViewer
{

    public class LogPakFileInfo
    {
        public string FilePath { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public string Sha1 { get; set; }
        public string Compression { get; set; }

        public LogPakFileInfo(string logEntry)
        {
            // Regular expression to extract the information from the log entry
            var regex = new Regex(@"LogPakFile: Display: ""(?<FilePath>.+?)"" offset: (?<Offset>\d+), size: (?<Size>\d+) bytes, sha1: (?<Sha1>[A-Fa-f0-9]+), compression: (?<Compression>.+?)\.");
            var match = regex.Match(logEntry);

            if (match.Success)
            {
                FilePath = match.Groups["FilePath"].Value;
                Offset = long.Parse(match.Groups["Offset"].Value);
                Size = long.Parse(match.Groups["Size"].Value);
                Sha1 = match.Groups["Sha1"].Value;
                Compression = match.Groups["Compression"].Value;
            }
            else
            {
                //throw new ArgumentException("Invalid log entry format");
            }
        }

        public override string ToString()
        {
            return $"FilePath: {FilePath}, Offset: {Offset}, Size: {Size} bytes, Sha1: {Sha1}, Compression: {Compression}";
        }
    }

    public partial class Form1 : Form
    {

        List<LogPakFileInfo> logPakFileInfos = new List<LogPakFileInfo>();

        private Dictionary<string, TreeNode> _directoryNodes = new Dictionary<string, TreeNode>();
        private Dictionary<string, LogPakFileInfo> _fileInfos = new Dictionary<string, LogPakFileInfo>();

        public Form1()
        {
            InitializeComponent();
            AllowDrop = true;

            // Initialize SplitContainer
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };

            // Initialize TreeView
            treeView1.Dock = DockStyle.Fill;

            // Initialize ListView

            listView1.Dock = DockStyle.Fill;
            listView1.View = View.Details;


            listView1.Columns.Add("File Path", 200);
            listView1.Columns.Add("Offset", 100);
            listView1.Columns.Add("Size", 100);
            listView1.Columns.Add("SHA1", 150);
            listView1.Columns.Add("Compression", 100);

            // Add controls to form
            splitContainer.Panel1.Controls.Add(treeView1);
            splitContainer.Panel2.Controls.Add(listView1);
            this.Controls.Add(splitContainer);

        }

        private void ProcessData(ref string data)
        {
            string[] lines = data.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (line.StartsWith("LogPakFile"))
                {
                    LogPakFileInfo logPakFileInfo = new LogPakFileInfo(line);
                    logPakFileInfos.Add(logPakFileInfo);
                }
            }
        }

        private void RefreshView()
        {
            if (logPakFileInfos.Count > 0)
            {
                PopulateTreeView();
                PopulateListView();

            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {


            if (e.Data != null)
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                foreach (string file in files)
                {
                    if (file.EndsWith(".pak"))
                    {
                        OpenPakFile(file);

                        // only first file is processed
                        break;
                    }
                }
            }
        }
        private string GetTreeNodePath(TreeNode node)
        {
            if (node == null)
                return "";

            string path = (node.Parent == null ? "" : GetTreeNodePath(node.Parent) + "/") + node.Text;
            return path;
        }

        private void PopulateTreeView()
        {
            foreach (var entry in logPakFileInfos)
            {
                if (entry.FilePath == null)
                    continue;

                // Split the path into directories
                var directories = entry.FilePath.Split('/');

                string currentPath = "";
                TreeNode currentNode = null;

                for (int i = 0; i < directories.Length; i++)
                {
                    var dir = directories[i];
                    currentPath += dir + "/";

                    // If it's the last part and it's a file (contains extension), skip adding it
                    if (i == directories.Length - 1 && Path.HasExtension(dir))
                    {
                        _fileInfos[entry.FilePath] = entry;
                        continue;
                    }

                    if (!_directoryNodes.ContainsKey(currentPath))
                    {
                        var newNode = new TreeNode(dir);
                        if (currentNode == null)
                        {
                            treeView1.Nodes.Add(newNode);
                            _directoryNodes[currentPath] = newNode;
                            currentNode = newNode;
                        }
                        else
                        {
                            currentNode.Nodes.Add(newNode);
                            _directoryNodes[currentPath] = newNode;
                            currentNode = newNode;
                        }
                    }
                    else
                    {
                        currentNode = _directoryNodes[currentPath];
                    }
                }
            }
        }



        private void PopulateListView()
        {

            // Add columns to the ListView
            listView1.Columns.Add("File Path", 200);
            listView1.Columns.Add("Offset", 100);
            listView1.Columns.Add("Size", 100);
            listView1.Columns.Add("SHA1", 150);
            listView1.Columns.Add("Compression", 100);


        }



        public void OpenPakFile(string file)
        {
            // get address from environment variable
            string address = Environment.GetEnvironmentVariable("unrealPak");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "unrealpak.exe", // Path to your console application
                Arguments = "-list " + file,  // Arguments for your console application, if any
                RedirectStandardOutput = true,      // Redirect the standard output
                UseShellExecute = false,            // Do not use the shell to execute
                CreateNoWindow = true               // Do not create a window

            };

            Process process = new Process
            {
                StartInfo = startInfo
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            ProcessData(ref output);
            RefreshView();
        }


        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            listView1.Items.Clear();
            string selectedDirectoryPath = GetTreeNodePath(e.Node).TrimEnd('/');
            foreach (var fileInfo in logPakFileInfos)
            {
                if (fileInfo.FilePath == null)
                {
                    continue;
                }
                string directoryPath = Path.GetDirectoryName(fileInfo.FilePath).Replace('\\', '/');
                if (directoryPath == selectedDirectoryPath)
                {
                    ListViewItem item = new ListViewItem(Path.GetFileName(fileInfo.FilePath));
                    item.SubItems.Add(fileInfo.Offset.ToString());
                    item.SubItems.Add(fileInfo.Size.ToString());
                    item.SubItems.Add(fileInfo.Sha1);
                    item.SubItems.Add(fileInfo.Compression);
                    listView1.Items.Add(item);
                }
            }
        }
    }
}
