using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LocalNicoMyList.DBAccessor;

namespace LocalNicoMyList
{
    class FolderItem : INotifyPropertyChanged
    {
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

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

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public FolderItem(FolderRecord folderRecord)
        {
            this.id = folderRecord.id;
            this.name = folderRecord.name;
            this.count = folderRecord.count;
        }

        public FolderItem(long folderId, string name)
        {
            this.id = folderId;
            this.name = name;
            this.count = 0;
        }
    }
}
