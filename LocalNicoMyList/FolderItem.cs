using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LocalNicoMyList.DBAccessor;

namespace LocalNicoMyList
{
    public class FolderItem : ViewModelBase
    {
        public long id;

        private string _name;
        public string name {
            get { return _name; }
            set
            {
                _name = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("name");
            }
        }

        private int _count;
        public int count {
            get { return _count; }
            set
            {
                _count = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("count");
            }
        }

        private bool _showedContextMenu;
        public bool showedContextMenu
        {
            get { return _showedContextMenu; }
            set
            {
                _showedContextMenu = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("showedContextMenu");
            }
        }

        private bool _isContextMenuCommandTarget;
        public bool isContextMenuCommandTarget
        {
            get { return _isContextMenuCommandTarget; }
            set
            {
                _isContextMenuCommandTarget = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("isContextMenuCommandTarget");
            }
        }

        public FolderItem(FolderRecord folderRecord, int count)
        {
            this.id = folderRecord.id;
            this.name = folderRecord.name;
            this.count = count;
        }

        public FolderItem(long folderId, string name)
        {
            this.id = folderId;
            this.name = name;
            this.count = 0;
        }
    }
}
