using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace truyenthanhServerWeb.Models
{
    public class User
    {
        public Account account { get; set; }

        public List<Device> lDevice;

        //just three element
        public List<byte[]> lADUBuffer = new List<byte[]>() { new byte[1], new byte[1], new byte[1]};

        //play control
        public enum ePlayCtrl { play, pause, stop};
        public ePlayCtrl playCtrl = ePlayCtrl.stop; //defaultvalue

        //list song in path song
        string pathSong = @"Data"; //combine with username in constructor
        List<string> lSong = new List<string>();

        public List<byte[]> lBuffer = new List<byte[]>();
        public byte[] bbuuff;

        public User(Account _account)
        {
            account = _account;

            for(int j = 0; j < 300000; j++)
            {
                var tmpbuff = new byte[1000];
                for (int i = 0; i < tmpbuff.Length; i++) bbuuff[i] = (byte)i;
                lBuffer.Add(tmpbuff);
            }
        }

        public void killUser()
        {
            bbuuff = null;
            lBuffer.Clear();
        }
    }
}
