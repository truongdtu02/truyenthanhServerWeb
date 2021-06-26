using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace truyenthanhServerWeb.Models
{
    public class User
    {
        public Account account { get; set; }
        public int indx; //index in _userList

        public List<Device> lDevice = new List<Device>();
        //public ObservableCollection<Device> lDevice = new ObservableCollection<Device>();

        //just three element
        public List<byte[]> lADUBuffer = new List<byte[]>() { new byte[1], new byte[1], new byte[1]};

        //play control
        public enum ePlayCtrl { play, pause, stop};
        public ePlayCtrl playCtrl = ePlayCtrl.stop; //defaultvalue

        //list song in path song
        string pathSong = @"Data"; //combine with username in constructor
        List<string> lSong = new List<string>();

        //public void killUser()
        //{
        //    bbuuff = null;
        //    lBuffer.Clear();
        //}   
    }
}
