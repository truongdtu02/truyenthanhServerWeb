using MP3_ADU_namespace;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using truyenthanhServerWeb.ServerMp3;

namespace truyenthanhServerWeb.Models
{
    public class User : UdpServer
    {
        public Account account { get; set; }
        public int indx; //index in _userList

        public List<Device> lDevice = new List<Device>();
        //public ObservableCollection<Device> lDevice = new ObservableCollection<Device>();

        //just three element
        public List<byte[]> lADUBuffer = new List<byte[]>() { new byte[1], new byte[1], new byte[1]};

        //play control
        public enum ePlayCtrl { play, pause, stop};
        public enum ePlayState { idle, running, pause};
        private ePlayCtrl playCtrl = ePlayCtrl.stop; //defaultvalue
        private ePlayState playState = ePlayState.idle; //defaultvalue

        //list song in path song
        public string pathSong;// = @"Data"; //combine with username in constructor
        public List<string> lSong = new List<string>();

        public event System.EventHandler<SongChangedEventArgs> SongChanged;
        public event System.EventHandler<ControlChangedEventArgs> ControlChanged;

        FFmpegXabe ffmpegXabe;// = new FFmpegXabe();

        private int ffmpegPort;
        public int FFmpegPort { get => ffmpegPort; }

        Thread ffmpegThread;

        private uint songId = 1;
        private uint frameId = 1;

        ADU_frame aduConvert = new ADU_frame();

        public void InvokeSongChangedEvent()
        {
            SongChanged?.Invoke(this, new SongChangedEventArgs());
        }
        public void InvokeControlChangedEvent(string _songName, User.ePlayCtrl _playCtrl)
        {
            ControlChanged?.Invoke(this, new ControlChangedEventArgs(_songName, _playCtrl));
        }

        // handler song control
        private void SongControlHandler(object sender, ControlChangedEventArgs args)
        {
            if (args.PlayCtrl == ePlayCtrl.play)
            {
                if (playState == ePlayState.idle)
                {
                    PlayNewSong(Path.Combine(pathSong, args.SongName));
                }
            }
            else if (args.PlayCtrl == ePlayCtrl.stop)
            {
                if(ffmpegXabe != null)
                    ffmpegXabe.StopConversion();
                playState = ePlayState.idle;
            }
        }

        private void PlayNewSong(string songPath)
        {
            ffmpegXabe = new FFmpegXabe();

            //check song exits
            if (File.Exists(songPath) && !ffmpegXabe.bIsConversionRunning)
            {
                songId++;
                if (songId == 0) songId = 1;
                frameId = 1;
                playState = ePlayState.running;

                ffmpegThread = new Thread(async () =>
                {
                    try
                    {
                        await ffmpegXabe.convertMP3(songPath, ffmpegPort);
                    }
                    catch { }
                });
                ffmpegThread.Start();
            }
        }

        public User(IPAddress address, int port, int _indx) : base(address, port)
        {
            ffmpegPort = port;
            Start();
            ControlChanged += SongControlHandler;
            indx = _indx;
            pathSong = Path.Combine(UDPServer.PathSong, indx.ToString());
            Console.WriteLine("user {0} path: {1}", indx, pathSong);
        }

        protected override void OnStarted()
        {
            // Start receive datagrams
            ReceiveAsync();
        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            //Console.WriteLine("Incoming: " + Encoding.UTF8.GetString(buffer, (int)offset, (int)size));
            //Console.WriteLine("Incoming: {0} {1}", (int)offset, (int)size);

            if(playState == ePlayState.running && size > 143) // ~ >= 144 min 48kbps
            {
                //packet frame
                byte[] sendADU = packet_udp_frameADU(buffer, (int)size);
                //sendAsync
                if(sendADU != null)
                {
                    foreach (var dv in lDevice)
                    {
                        if (dv.deviceEndpoint.On && !dv.deviceEndpoint.TimeOut)
                        {
                            SendAsync(dv.deviceEndpoint.IPEndPoint_client, sendADU);
                        }
                    }
                    frameId++;
                }
            }

            // Echo the message back to the sender
            //SendAsync(endpoint, buffer, 0, size);
            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"UDP FFmpeg {indx} : {error}");
        }

        private byte[] packet_udp_frameADU(byte[] buffer, int size)
        {
            if (frameId == 1)
            {
                aduConvert.CheckFrame(buffer, size); //update mp3 frame infor
                aduConvert.Reset();
            }

            byte[] aduFrame = aduConvert.ReadNextADUFrame(buffer, size);
            if (aduFrame == null) return null;

            int sizeOfADUpacket = aduFrame.Length + 2 + 4 + 4; //2B checksum, 4B id adu frame, 4B idSong
            byte[] tmpADUpacket = new byte[sizeOfADUpacket];
            //copy adu id
            System.Buffer.BlockCopy(BitConverter.GetBytes(frameId), 0, tmpADUpacket, 2, 4); //start from 1
            //copy song id
            System.Buffer.BlockCopy(BitConverter.GetBytes(songId), 0, tmpADUpacket, 2 + 4, 4);
            //copy adu data
            System.Buffer.BlockCopy(aduFrame, 0, tmpADUpacket, 2 + 4 + 4, aduFrame.Length);
            //checksum
            UInt16 checkSum = CaculateChecksum(tmpADUpacket, 2, sizeOfADUpacket - 2);
            //copy checksum
            System.Buffer.BlockCopy(BitConverter.GetBytes(checkSum), 0, tmpADUpacket, 0, 2);

            return tmpADUpacket;
        }

        private UInt16 CaculateChecksum(byte[] data, int offset, int length)
        {
            UInt32 checkSum = 0;
            int index = offset;
            while (length > 1)
            {
                checkSum += ((UInt32)data[index] << 8) | ((UInt32)data[index + 1]); //little edian
                length -= 2;
                index += 2;
            }
            if (length == 1) // still have 1 byte
            {
                checkSum += ((UInt32)data[index] << 8);
            }
            while ((checkSum >> 16) > 0) //checkSum > 0xFFFF
            {
                checkSum = (checkSum & 0xFFFF) + (checkSum >> 16);
            }
            //inverse
            checkSum = ~checkSum;
            return (UInt16)checkSum;
        }

        //public void killUser()
        //{
        //    bbuuff = null;
        //    lBuffer.Clear();
        //}

        //public void SendArrayAsync(EndPoint endpoint, byte[] data, int offset, int length)
        //{
        //    if (offset + length > data.Length) return;
        //    SendAsync(endpoint, data, offset, length);
        //}

        //protected override void OnSent(EndPoint endpoint, long sent)
        //{
        //    // Continue receive datagrams
        //    ReceiveAsync();
        //}

    }

    public class SongChangedEventArgs : System.EventArgs
    {
        //public List<Device> NewValue { get; }

        //public DeviceChangedEventArgs(List<Device> newValue)
        public SongChangedEventArgs()
        {
            //this.NewValue = newValue;
        }
    }

    public class ControlChangedEventArgs : System.EventArgs
    {
        private string songName;
        public string SongName { get => songName; }
        private User.ePlayCtrl playCtrl;
        public User.ePlayCtrl PlayCtrl { get => playCtrl; }
        public ControlChangedEventArgs(string _songName, User.ePlayCtrl _playCtrl)
        {
            songName = _songName;
            playCtrl = _playCtrl;
        }
    }
}
