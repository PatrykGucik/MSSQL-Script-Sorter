using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

public struct CheckException
{
    public string TreeKeyValue;
    public string MissObjectValue;
};


namespace SQLScriptSorter
{


    public partial class MainForm : Form
    {
        List<string> SQLFileList = new List<string>();
        List<string> NotCheckedId = new List<string>();
        List<string> NotCheckedValue = new List<string>();
        Tokens FTokenLst = new Tokens();
        bool FChangeExecuting = false;



        public MainForm()
        {
            InitializeComponent();
        }

        private void OpenFileButton_Click(object sender, EventArgs e)
        {
            Stream myStream = null;
            if (eOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                SQLFileList.Clear();
                eScript.Text = "";
                progressBar1.Value = 0;
                LeftTree.Nodes.Clear();
                try
                {
                    FilePathBox.Text = eOpenFileDialog.FileName;
                    CompileButton.Enabled = true;
                    if ((myStream = eOpenFileDialog.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            StreamReader sr = new StreamReader(myStream, Encoding.GetEncoding("windows-1250"));
                            while (!sr.EndOfStream) SQLFileList.Add(sr.ReadLine());
                            sr.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void CompileButton_Click(object sender, EventArgs e)
        {
            XmlDataDocument xmldoc = new XmlDataDocument();
            XmlNodeList xmlnode, xmlCheck;
            FTokenLst.Clear();
            string StartupPath = Application.StartupPath;
            if (StartupPath[StartupPath.Length - 1] != '\\') StartupPath += '\\';
            FileStream fs = new FileStream(StartupPath + "SQLScriptSorterConfig.xml", FileMode.Open, FileAccess.Read);
            xmldoc.Load(fs);
            xmlnode = xmldoc.GetElementsByTagName("SQLScript");
            for (int i = 0; i < xmlnode.Count; i++)
            {
                XmlNode XmlN = xmlnode[i];
                TreeNode TreeN = LeftTree.Nodes.Add(XmlN.Attributes["TreeName"].Value);
                switch (XmlN.Attributes["TreeKey"].Value.ToUpper())
                {
                    case "FUN":
                        TreeN.ImageIndex = 0;
                        break;
                    case "VIEW":
                        TreeN.ImageIndex = 3;
                        break;
                    case "TABLE":
                        TreeN.ImageIndex = 2;
                        break;
                    case "PROC":
                        TreeN.ImageIndex = 1;
                        break;
                }
                TreeN.StateImageIndex = TreeN.SelectedImageIndex = TreeN.ImageIndex;
                FTokenLst.Add(new TokenItem(TreeN, XmlN.InnerText));
            }

            xmlCheck = xmldoc.GetElementsByTagName("MissObject");

            for (int i = 0; i < xmlCheck.Count; i++)
            {
                XmlNode CheckXml = xmlCheck[i];

                for (int j = 0; j < xmlnode.Count; j++)
                {
                    XmlNode XmlN = xmlnode[j];
                    if (CheckXml.Attributes["TreeKey"].Value == XmlN.Attributes["TreeKey"].Value)
                    {
                        NotCheckedId.Add(XmlN.Attributes["TreeName"].Value);
                        break;
                    }
                }
                eScript.Text += NotCheckedId[i].ToString() + "\n";
                NotCheckedValue.Add(CheckXml.InnerText);
                eScript.Text += NotCheckedValue[i].ToString() + "\n";
            }
            fs.Close();

            string ScriptTxt = "";
            ScriptItem ScriptI = null;
            for (int i = 0; i < SQLFileList.Count; i++)
            {
                string LineTxt = SQLFileList[i].ToUpper();
                string ScriptName = SQLFileList[i];
                if (LineTxt.Length > 6 && LineTxt.Substring(0, 6) == "CREATE")
                {
                    ScriptTxt = "";
                    string t1 = LineTxt.Remove(0, 7);
                    ScriptName = ScriptName.Remove(0, 7);
                    int SpacePos = t1.IndexOf(" ");
                    if (SpacePos > 0)
                    {
                        string TokenKind = t1.Substring(0, SpacePos);
                        TokenItem TokenI = FTokenLst.GetItem(TokenKind);
                        if (TokenI != null)
                        {
                            ScriptI = new ScriptItem(ScriptName);
                            TokenI.Scripts.Add(ScriptI);
                        }
                    }
                }
                if (SQLFileList[i].Trim() != "")
                {
                    if (ScriptTxt != "") ScriptTxt += Environment.NewLine;
                    ScriptTxt += SQLFileList[i];
                }
                if (SQLFileList[i] == "GO")
                {
                    if (ScriptI != null) ScriptI.Text = ScriptTxt;
                    ScriptI = null;
                    ScriptTxt = "";
                }
            }
            LeftTree.BeginUpdate();
            FTokenLst.BuildTree();
            foreach (TreeNode N in LeftTree.Nodes)
            {
                N.Expand();
            }
            LeftTree.EndUpdate();
            SaveCompileTextButton.Enabled = true;
            CompileButton.Enabled = false;
        }

        private void SaveCompileTextButton_Click(object sender, EventArgs e)
        {
            progressBar1.Maximum = 0;
            foreach (TreeNode parent in LeftTree.Nodes)
            {
                foreach (TreeNode child in LeftTree.Nodes[parent.Index].Nodes)
                {
                    foreach (TreeNode element in LeftTree.Nodes[parent.Index].Nodes[child.Index].Nodes)
                    {
                        if (element.Checked)
                            progressBar1.Maximum += 1;
                    }
                }
            }
            if (progressBar1.Maximum == 0)
                MessageBox.Show("Żaden skrypt nie został zaznaczony");
            else
            {
                if (eSaveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    StreamWriter writer = new StreamWriter(eSaveFileDialog.OpenFile());
                    foreach (TreeNode parent in LeftTree.Nodes)
                    {
                        foreach (TreeNode child in LeftTree.Nodes[parent.Index].Nodes)
                        {
                            foreach (TreeNode element in LeftTree.Nodes[parent.Index].Nodes[child.Index].Nodes)
                            {
                                if (element.Checked)
                                {
                                    writer.WriteLine((element.Tag as ScriptItem).Text);
                                    progressBar1.Increment(1);
                                }
                            }
                        }
                    }
                    writer.Dispose();
                    writer.Close();
                    MessageBox.Show("Zapis wykonano poprawnie");
                }
            }
        }

        private void SortButton_Click(object sender, EventArgs e)
        {
            LeftTree.Sort();
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (FChangeExecuting) return;
            FChangeExecuting = true;
            LeftTree.SelectedNode = e.Node;
            LeftTree.BeginUpdate();
            CheckAllChildNodes(e.Node, e.Node.Checked);
            LeftTree.EndUpdate();
            FChangeExecuting = false;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null) eScript.Text = "";
            else eScript.Text = (e.Node.Tag as ScriptItem).Text;
        }

        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                if (node.Nodes.Count == 0)
                {
                    for (int i = 0; i < NotCheckedId.Count; i++)
                    {
                        string text = node.Text;
                        if (text != NotCheckedId[i])
                        {
                            if (text.Count() >= NotCheckedValue[i].Count())
                            {
                                if (text.Count() > NotCheckedValue[i].Count())
                                {
                                    text = text.Substring(0, NotCheckedValue[i].Count());
                                }
                            }
                            if (text.ToLower() == NotCheckedValue[i].ToLower() && node.Parent.Parent.Text.ToLower() == NotCheckedId[i].ToLower())
                            {
                                node.Checked = false;
                                break;
                            }
                            else
                            {
                                node.Checked = nodeChecked;
                            }
                        }
                    }
                }
                else
                {
                    node.Checked = nodeChecked;
                    if (node.Nodes.Count > 0)
                    {
                        this.CheckAllChildNodes(node, nodeChecked);
                    }
                }
            }
        }
    }
}

