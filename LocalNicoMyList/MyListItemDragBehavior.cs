using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace LocalNicoMyList
{
    class MyListItemDragBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            this.AssociatedObject.PreviewMouseLeftButtonDown += previewMouseLeftDown;
            this.AssociatedObject.PreviewMouseLeftButtonUp += previewMouseLeftUp;
            this.AssociatedObject.PreviewMouseMove += previewMouseMove;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewMouseLeftButtonDown -= previewMouseLeftDown;
            this.AssociatedObject.PreviewMouseLeftButtonUp -= previewMouseLeftUp;
            this.AssociatedObject.PreviewMouseMove -= previewMouseMove;
        }

        Point? _mouseDownPt = null;
        MyListItem _draggedItem;
        bool _preventSelectionChangeOnLeftMouseDown;
        DragDropKeyStates _keyStateOnPreviewMouseDown;

        private void previewMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ListView;

            _preventSelectionChangeOnLeftMouseDown = false;
            _mouseDownPt = null;

            var lvi = listView.ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            if (null == lvi)
                return;

            _mouseDownPt = e.GetPosition(listView);
            _draggedItem = lvi.DataContext as MyListItem;

            // 選択されているアイテムのdownで選択されてしまうと複数アイテムのドラッグができないので選択されるのを抑制
            bool multiSelect = listView.SelectedItems.Count > 1;
            _keyStateOnPreviewMouseDown = 0;
            _keyStateOnPreviewMouseDown |= (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ? DragDropKeyStates.ControlKey : 0;
            _keyStateOnPreviewMouseDown |= (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ? DragDropKeyStates.ShiftKey : 0;

            MyListItem myListItem = lvi?.DataContext as MyListItem;
            // Shiftが押されている場合は抑制しない
            if (0 != (_keyStateOnPreviewMouseDown & DragDropKeyStates.ShiftKey))
                return;
            // Ctrlが押されている場合はdownで選択状態変更を抑制
            if (0 != (_keyStateOnPreviewMouseDown & DragDropKeyStates.ControlKey))
            {
                _preventSelectionChangeOnLeftMouseDown = true;
                e.Handled = true;
            }
            // Ctrl,Shiftが押されていない場合、複数選択されていて選択されているアイテムがdownされる場合は抑制
            else if (multiSelect && listView.SelectedItems.Contains(myListItem))
            {
                _preventSelectionChangeOnLeftMouseDown = true;
                e.Handled = true;
            }
        }

        private void previewMouseMove(object sender, MouseEventArgs e)
        {
            var listView = sender as ListView;
            var lvi = listView.ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;

            if (e.LeftButton != MouseButtonState.Pressed || !_mouseDownPt.HasValue)
            {
                return;
            }

            var point = e.GetPosition(listView);
            if (this.checkDistance(point, _mouseDownPt.Value))
            {
                // Ctrlを押しながら未選択のアイテムをdownしてmoveした場合、downで選択状態の変更を抑制しているため
                // downされたアイテムが選択状態にならないので、ここで選択状態にする。
                if (0 != (_keyStateOnPreviewMouseDown & DragDropKeyStates.ControlKey))
                {
                    if (!listView.SelectedItems.Contains(_draggedItem))
                        listView.SelectedItems.Add(_draggedItem);
                }

                _preventSelectionChangeOnLeftMouseDown = false; // MouseUpで選択状態を変更しないように

                DragDrop.DoDragDrop(lvi, _draggedItem, DragDropEffects.Move | DragDropEffects.Copy);

                _mouseDownPt = null;
                _draggedItem = null;

                e.Handled = true;
            }
        }

        private void previewMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ListView;
            var lvi = listView.ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;

            _mouseDownPt = null;
            _draggedItem = null;

            if (_preventSelectionChangeOnLeftMouseDown)
            {
                // Ctrlが押されている場合は抑制したので、ここで選択状態を変更する
                MyListItem myListItem = lvi?.DataContext as MyListItem;
                if (0 != (_keyStateOnPreviewMouseDown & DragDropKeyStates.ControlKey))
                {
                    if (listView.SelectedItems.Contains(myListItem))
                        listView.SelectedItems.Remove(myListItem);
                    else
                        listView.SelectedItems.Add(myListItem);
                }
                else
                {
                    // Ctrl,Shiftが押されていない場合は抑制したので、ここで選択する
                    listView.SelectedItems.Clear();
                    listView.SelectedItems.Add(myListItem);
                }

            }
        }

        private bool checkDistance(Point x, Point y)
        {
            return Math.Abs(x.X - y.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(x.Y - y.Y) >= SystemParameters.MinimumVerticalDragDistance;
        }
    }
}
