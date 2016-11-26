using System;
using System.Windows;

namespace DragDropListView
{
    /// <summary>
    /// AcceptDropビヘイビアに渡す定義を保持するクラス
    /// </summary>
    public class AcceptDropSpecifications
    {
        public event Action<object, DragEventArgs> DragEnter;

        public void OnDragEnter(object sender, DragEventArgs args)
        {
            var handler = DragEnter;

            if (handler != null)
                handler(sender, args);
        }

        public event Action<object, DragEventArgs> DragLeave;

        public void OnDragLeave(object sender, DragEventArgs args)
        {
            var handler = DragLeave;

            if (handler != null)
                handler(sender, args);
        }

        public event Action<object, DragEventArgs> DragOver;

        public void OnDragOver(object sender, DragEventArgs args)
        {
            var handler = DragOver;

            if (handler != null)
                handler(sender, args);
        }

        public event Action<object, DragEventArgs> DragDrop;

        public void OnDrop(object sender, DragEventArgs args)
        {
            var handler = DragDrop;

            if (handler != null)
                handler(sender, args);
        }
    }
}
