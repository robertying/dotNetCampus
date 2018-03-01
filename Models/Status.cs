using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampusNet
{
    public class Status : ObservableObject
    {
        private string usage;
        private string network;
        private string username;
        private string balance;
        private string session;

        public Status()
        {
            Usage = "--";
            Network = "--";
            Username = "--";
            Balance = "--";
            Session = "--";
        }

        public string Network
        {
            get => network;
            set
            {
                if (value != network)
                {
                    network = value;
                    OnPropertyChanged("Network");
                }
            }
        }

        public string Username
        {
            get => username;
            set
            {
                if (value != username)
                {
                    username = value;
                    OnPropertyChanged("Username");
                }
            }
        }

        public string Balance
        {
            get => balance;
            set
            {
                if (value != balance)
                {
                    balance = value;
                    OnPropertyChanged("Balance");
                }
            }
        }

        public string Usage
        {
            get => usage;
            set
            {
                if (value != usage)
                {
                    usage = value;
                    OnPropertyChanged("Usage");
                }
            }
        }

        public string Session
        {
            get => session;
            set
            {
                if (value != session)
                {
                    session = value;
                    OnPropertyChanged("Session");
                }
            }
        }
    }
}
