using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SQLScriptSorter {
  public class Tokens : List<TokenItem> {
    public TokenItem GetItem(string AName) {
      foreach (TokenItem Item in this) {
        if (Item.CheckName(AName)) return Item;
      }
      return null;
    }

    public void BuildTree() {
      foreach (TokenItem Item in this) {
        Item.BuildTree();
      }
    }
  }

  public class TokenItem {
    private TreeNode FNode;
    private string[] FItems;
    private List<ScriptItem> FScriptNames = new List<ScriptItem>();

    private TreeNode CreateTreeNode(TreeNode AParent, string AText, int AIndex = -1) {
      TreeNode N;
      if (AIndex < 0) N = AParent.Nodes.Add(AText);
      else N = AParent.Nodes[AIndex].Nodes.Add(AText);
      N.ImageIndex = N.StateImageIndex = N.SelectedImageIndex = AParent.ImageIndex;
      return N;
    }

    public TokenItem(TreeNode ANode, string ATokens) {
      FNode = ANode;
      FItems = ATokens.Trim().Split(';');
    }

    public bool CheckName(string AName) {
      for (int i = 0; i < FItems.Length; i++) {
        if (AName.ToUpper() == FItems[i].ToUpper()) return true;
      }
      return false;
    }

    public void BuildTree() {
      foreach (ScriptItem Item in FScriptNames) {
        TreeNode N = null;
        for (int i = 0; i < FNode.Nodes.Count; i++) {
          if (Item.Prefix.ToUpper() == FNode.Nodes[i].Text.ToUpper()) {
            N = CreateTreeNode(FNode, Item.Name, i);
            N.Tag = Item;
            break;
          }
        }
        if (N == null) {
          N = CreateTreeNode(FNode, Item.Prefix);
          CreateTreeNode(N, Item.Name).Tag = Item;
        }
      }
    }

    public List<ScriptItem> Scripts { get { return FScriptNames; } }
  }

  public class ScriptItem {
    private string FPrefix;
    private string FName;

    public ScriptItem(string AName) {
      int i = AName.IndexOf('[');
      int j = AName.IndexOf(']');
      FPrefix = AName.Substring(i + 1, j - i - 1);
      AName = AName.Remove(0, j + 2);
      j = AName.IndexOf(']');
      FName = AName.Substring(1, j - 1);

      Text = "";
    }

    public string Prefix { get { return FPrefix; } }
    public string Name { get { return FName; } }
    public string Text { get; set; }
  }
}
