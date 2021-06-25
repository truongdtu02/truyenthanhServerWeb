using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace truyenthanhServerWeb.Models
{
    public class User
    {
        public Account account { get; set; }
        public int indx; //index in _userList

        public List<Device> lDevice = new List<Device>();

        //just three element
        public List<byte[]> lADUBuffer = new List<byte[]>() { new byte[1], new byte[1], new byte[1]};

        //play control
        public enum ePlayCtrl { play, pause, stop};
        public ePlayCtrl playCtrl = ePlayCtrl.stop; //defaultvalue

        //list song in path song
        string pathSong = @"Data"; //combine with username in constructor
        List<string> lSong = new List<string>();

        //public User(Account _account)
        //{
        //    account = _account;
        //}

        //public void killUser()
        //{
        //    bbuuff = null;
        //    lBuffer.Clear();
        //}   
    }
}
