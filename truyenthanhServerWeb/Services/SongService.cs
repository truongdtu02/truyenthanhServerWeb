using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using truyenthanhServerWeb.Models;
using truyenthanhServerWeb.ServerMp3;

namespace truyenthanhServerWeb.Services
{
    public class SongService
    {
        //private readonly int _userIndx;
        //public int UserIndx { get => _userIndx; }
        //private readonly IHttpContextAccessor _httpContextAccessor;
        //public SongService(IHttpContextAccessor httpContextAccessor)
        //{
        //    _httpContextAccessor = httpContextAccessor;

        //    string tmpUsrname = _httpContextAccessor.HttpContext.User.Identity.Name;
        //    _userIndx = UDPServer._userList.FindLastIndex(x => x.account.Username == tmpUsrname);

        //}
        //

        public SongService()
        {

        }

        public int GetIndxByUsername(string userName)
        {
            return UDPServer._userList.FindLastIndex(x => x.account.Username == userName);
        }

        public List<string> Get(int _userIndx)
        {
            if(_userIndx != -1)
            {
                return UDPServer._userList[_userIndx].lSong;
            }
            else
            {
                return null;
            }
        }

        public void Add(string songName, int _userIndx)
        {
            if(_userIndx != -1)
            {
                //check duplicate name song
                if (!UDPServer._userList[_userIndx].lSong.Contains(songName))
                    UDPServer._userList[_userIndx].lSong.Add(songName);

                //synchronize between disk and lSong
                Synchronize(_userIndx);

                UDPServer._userList[_userIndx].InvokeListSongChangedEvent();
            }
        }

        public string GetRootPath(int _userIndx)
        {
            if(_userIndx != -1)
            {
                return UDPServer._userList[_userIndx].pathSong;
            }
            return null;
        }

        //change order in list
        public void ChangeOrderUp(string _songName, int _userIndx)
        {
            if (_userIndx != -1)
            {
                int tmpSongIndx = UDPServer._userList[_userIndx].lSong.FindLastIndex(x => x == _songName);
                if (tmpSongIndx > 0) // != 0 and != -1
                {
                    UDPServer._userList[_userIndx].lSong.RemoveAt(tmpSongIndx);
                    UDPServer._userList[_userIndx].lSong.Insert(tmpSongIndx - 1, _songName);

                }
                UDPServer._userList[_userIndx].InvokeListSongChangedEvent();
            }
        }

        public void ChangeOrderDown(string _songName, int _userIndx)
        {
            if (_userIndx != -1)
            {
                int tmpSongIndx = UDPServer._userList[_userIndx].lSong.FindLastIndex(x => x == _songName);
                if (tmpSongIndx > -1 && tmpSongIndx < (UDPServer._userList[_userIndx].lSong.Count - 1)) // != -1 and != Count -1 (last item)
                {
                    UDPServer._userList[_userIndx].lSong.RemoveAt(tmpSongIndx);
                    UDPServer._userList[_userIndx].lSong.Insert(tmpSongIndx + 1, _songName);
                }

                UDPServer._userList[_userIndx].InvokeListSongChangedEvent();
            }
        }

        public void Delete(string _songName, int _userIndx)
        {
            if (_userIndx != -1)
            {
                int tmpSongIndx = UDPServer._userList[_userIndx].lSong.FindLastIndex(x => x == _songName);
                if (tmpSongIndx != -1)
                {
                    string tmpPathDel = Path.Combine(UDPServer._userList[_userIndx].pathSong, _songName);
                    if (File.Exists(tmpPathDel))
                    {
                        try
                        {
                            File.Delete(tmpPathDel);
                            UDPServer._userList[_userIndx].lSong.RemoveAt(tmpSongIndx);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }

                UDPServer._userList[_userIndx].InvokeListSongChangedEvent();
            }
        }

        //delete song is not both exist in lSong and disk
        public void Synchronize(int _userIndx)
        {
            List<string> saveSongInDisk = new List<string>();
            string tmpPathsong = UDPServer._userList[_userIndx].pathSong;

            //disk -> lSong
            if(Directory.Exists(tmpPathsong))
            {
                DirectoryInfo di = new DirectoryInfo(tmpPathsong);
                foreach (FileInfo file in di.GetFiles())
                {
                    //check file is supported type
                    if (!UDPServer.TypeFileSupport.Contains(file.Extension))
                    {
                        file.Delete();
                    }
                    else
                    {
                        //check List song, if don't have delete (since uploading is interrupted, so namefile is not added to lSong
                        if (!UDPServer._userList[_userIndx].lSong.Contains(file.Name))
                            //UDPServer._userList[_userIndx].lSong.Add(file.Name);
                            file.Delete();

                        saveSongInDisk.Add(file.Name);
                    }
                }
            }

            for (int i = UDPServer._userList[_userIndx].lSong.Count - 1; i >= 0; i--)
            {
                if (!saveSongInDisk.Contains(UDPServer._userList[_userIndx].lSong[i]))
                    UDPServer._userList[_userIndx].lSong.RemoveAt(i);
            }
        }

        //song control
        public void Play(string _songName, int _userIndx)
        {
            if(_userIndx != -1)
            {
                UDPServer._userList[_userIndx].InvokeControlChangedEvent(_songName, User.ePlayCtrl.play);
            }
        }
        public void Pause(int _userIndx)
        {
            if (_userIndx != -1)
                UDPServer._userList[_userIndx].InvokeControlChangedEvent(null, User.ePlayCtrl.pause);
        }
        public void Stop(int _userIndx)
        {
            if (_userIndx != -1)
                UDPServer._userList[_userIndx].InvokeControlChangedEvent(null, User.ePlayCtrl.stop);
        }
        public void PlayBackAllChange(bool _playBack, bool _playAll, int _userIndx)
        {
            if (_userIndx != -1)
                UDPServer._userList[_userIndx].PlayBackAllChange(_playBack, _playAll);
        }
    }
}
