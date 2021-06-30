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

        Thread ffmpegThread, listenUDPThread;

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
                        playState = ePlayState.idle;
                    }
                    catch { }
                });
                ffmpegThread.Start();
            }
        }

        //socket udp listen data from ffmpeg
        // InterNetwork là họ địa chỉ dành cho IPv4
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        // biến này về sau sẽ chứa địa chỉ của tiến trình client nào gửi gói tin tới
        EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);

        byte[] receiveBuffer = new byte[144]; //1 frame 48kbps

        public User(int _indx, Account _ac)
        {
            ffmpegPort = UDPServer.PortFFmpeg + _indx;
            indx = _indx;
            account = _ac;

            ControlChanged += SongControlHandler;
            pathSong = Path.Combine(UDPServer.PathSong, indx.ToString());

            socket.Bind(new IPEndPoint(IPAddress.Loopback, ffmpegPort));

            //initiallize listen UDP thread
            listenUDPThread = new Thread(() =>
            {
                while(true)
                {
                    try
                    {
                        // khi nhận được gói tin nào sẽ lưu lại địa chỉ của tiến trình client
                        var length = socket.ReceiveFrom(receiveBuffer, ref remoteEndpoint);
                        //Console.WriteLine("r {0}", length);
                        HandleReceived(length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("UDP user {0} error {1}", indx, ex);
                    }
                }
            });
            listenUDPThread.Priority = ThreadPriority.AboveNormal;
            listenUDPThread.Start();
        }

        private void HandleReceived(int length)
        {
            if(playState == ePlayState.running && length == 144) // ~ >= 144 min 48kbps
            {
                //packet frame
                int packetLength = packet_udp_frameADU(receiveBuffer, length);
                //sendAsync
                if(packetLength > 0)
                {
                    foreach (var dv in lDevice)
                    {
                        if (dv.deviceEndpoint.On && !dv.deviceEndpoint.TimeOut)
                        {
                            UDPServer.SendSync(dv.deviceEndpoint.IPEndPoint_client, sendBuff, 0, packetLength);
                            //Send(dv.deviceEndpoint.IPEndPoint_client, sendBuff, 0, packetLength);
                            //SendAsync(dv.deviceEndpoint.IPEndPoint_client, sendBuff, 0, packetLength);
                            //Console.WriteLine("s {0} {1}", dv.Name, packetLength);
                        }
                    }
                    frameId++;
                }
            }

            if (length != 144) Console.WriteLine("error size {0}", length);
        }

        byte[] sendBuff = new byte[1000];

        //return size of adu packet
        private int packet_udp_frameADU(byte[] buffer, int size)
        {
            if (frameId == 1)
            {
                aduConvert.CheckFrame(buffer, size); //update mp3 frame infor
                aduConvert.Reset();
            }

            byte[] aduFrame = aduConvert.ReadNextADUFrame(buffer, size);
            if (aduFrame == null) return -1;

            int sizeOfADUpacket = aduFrame.Length + 2 + 4 + 4; //2B checksum, 4B id adu frame, 4B idSong
            //copy adu id
            System.Buffer.BlockCopy(BitConverter.GetBytes(frameId), 0, sendBuff, 2, 4); //start from 1
            //copy song id
            System.Buffer.BlockCopy(BitConverter.GetBytes(songId), 0, sendBuff, 2 + 4, 4);
            //copy adu data
            System.Buffer.BlockCopy(aduFrame, 0, sendBuff, 2 + 4 + 4, aduFrame.Length);
            //checksum
            UInt16 checkSum = CaculateChecksum(sendBuff, 2, sizeOfADUpacket - 2);
            //copy checksum
            System.Buffer.BlockCopy(BitConverter.GetBytes(checkSum), 0, sendBuff, 0, 2);

            return sizeOfADUpacket;
        }


        //private byte[] packet_udp_frameADU(byte[] buffer, int size)
        //{
        //    if (frameId == 1)
        //    {
        //        aduConvert.CheckFrame(buffer, size); //update mp3 frame infor
        //        aduConvert.Reset();
        //    }

        //    byte[] aduFrame = aduConvert.ReadNextADUFrame(buffer, size);
        //    if (aduFrame == null) return null;

        //    int sizeOfADUpacket = aduFrame.Length + 2 + 4 + 4; //2B checksum, 4B id adu frame, 4B idSong
        //    byte[] tmpADUpacket = new byte[sizeOfADUpacket];
        //    //copy adu id
        //    System.Buffer.BlockCopy(BitConverter.GetBytes(frameId), 0, tmpADUpacket, 2, 4); //start from 1
        //    //copy song id
        //    System.Buffer.BlockCopy(BitConverter.GetBytes(songId), 0, tmpADUpacket, 2 + 4, 4);
        //    //copy adu data
        //    System.Buffer.BlockCopy(aduFrame, 0, tmpADUpacket, 2 + 4 + 4, aduFrame.Length);
        //    //checksum
        //    UInt16 checkSum = CaculateChecksum(tmpADUpacket, 2, sizeOfADUpacket - 2);
        //    //copy checksum
        //    System.Buffer.BlockCopy(BitConverter.GetBytes(checkSum), 0, tmpADUpacket, 0, 2);

        //    return tmpADUpacket;
        //}

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
