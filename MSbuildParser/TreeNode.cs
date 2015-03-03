using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MSbuildParser
{
  public class TreeNode<T> : IEnumerable<TreeNode<T>>
  {

    public T Data { get; set; }

    public string Note { get; set; }

    public int BuildNumber
    {
      get
      {
        var root = this;
        while (!root.IsRoot)
        {
          root = root.Parent;
        }
        return Count(root, Note, Data);
      }
    }

    private static int Count(TreeNode<T> treeNode, string note, T data)
    {
      var count = 0;
      foreach (var child in treeNode.Children)
      {
        if (child.Data.ToString() == data.ToString() && child.Note == note)
          count ++;
        count += Count(child, note, data);
      }
      return count;
    }

    public bool IsClosed { get; set; }
    public TreeNode<T> Parent { get; set; }
    public ICollection<TreeNode<T>> Children { get; set; }

    public Boolean IsRoot
    {
      get { return Parent == null; }
    }

    public Boolean IsLeaf
    {
      get { return Children.Count == 0; }
    }

    public int Level
    {
      get
      {
        if (this.IsRoot)
          return 0;
        return Parent.Level + 1;
      }
    }


    public TreeNode(T data)
    {
      this.Data = data;
      this.Children = new LinkedList<TreeNode<T>>();

      this.ElementsIndex = new LinkedList<TreeNode<T>>();
      this.ElementsIndex.Add(this);
    }

    public TreeNode<T> AddChild(T child, string note)
    {

      TreeNode<T> childNode = new TreeNode<T>(child) {Parent = this, Note = note};
      this.Children.Add(childNode);
      this.RegisterChildForSearch(childNode);
      return childNode;
    }

    public override string ToString()
    {
      return Data != null ? Data.ToString() : "[data null]";
    }

    public void Close()
    {
      this.IsClosed = true;
      foreach (var child in this.Children)
      {
        if (!child.IsClosed)
          child.Close();
      }
    }

    #region searching

    private ICollection<TreeNode<T>> ElementsIndex { get; set; }

    private void RegisterChildForSearch(TreeNode<T> node)
    {
      ElementsIndex.Add(node);
      if (Parent != null)
        Parent.RegisterChildForSearch(node);
    }

    public TreeNode<T> FindTreeNode(Func<TreeNode<T>, bool> predicate)
    {
      return this.ElementsIndex.FirstOrDefault(predicate);
    }

    #endregion


    #region iterating

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IEnumerator<TreeNode<T>> GetEnumerator()
    {
      yield return this;
      foreach (var directChild in this.Children)
      {
        foreach (var anyChild in directChild)
          yield return anyChild;
      }
    }

    #endregion
  }
}