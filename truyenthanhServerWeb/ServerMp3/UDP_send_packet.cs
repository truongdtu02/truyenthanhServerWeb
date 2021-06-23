#define DEBUG

using MP3_ADU_namespace;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UDP_send_packet_frame
{
    class MulticastUDPServer : UdpServer
    {
        public MulticastUDPServer(IPAddress address, int port) : base(address, port)
        {
            //Start();
        }

        //protected override void OnStarted()
        //{
        //    // Start receive datagrams
        //    ReceiveAsync();
        //}

        //protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        //{
        //    //Console.WriteLine("Incoming: " + Encoding.UTF8.GetString(buffer, (int)offset, (int)size));


        //    // Echo the message back to the sender
        //    //SendAsync(endpoint, buffer, 0, size);
        //    ReceiveAsync();
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

        //protected override void OnError(SocketError error)
        //{
        //    Console.WriteLine($"Echo UDP server caught an error with code {error}");
        //}
    }

    class UDPsocket
    {
        static bool left_frame_not_packet = false;
        static int sizeOfPacket;

        bool loopBack = true;
        public bool LoopBack { get => loopBack; set => loopBack = value; }

        //2 thread
        Thread threadListen, threadSend, threadCheckRequest;

        //socket UDP
        static IPAddress localIp = IPAddress.Any;
        static int localPort = 1308;
        static IPEndPoint localEndPoint = new IPEndPoint(localIp, localPort);
        //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        MulticastUDPServer socket;

        //for threadListen
        byte[] receive_buffer = new byte[30];
        // biến này về sau sẽ chứa địa chỉ của tiến trình client nào gửi gói tin tới
        EndPoint receive_IPEndPoint = new IPEndPoint(IPAddress.Any, 0);
        //List save client request
        List<client_IPEndPoint> clientList = new List<client_IPEndPoint>();
        public List<client_IPEndPoint> ClientList { get => clientList; set => clientList = value; }

        //save duration and time playing of current song
        int duration_song_s = 0;
        int timePlaying_song_s = 0;
        static int songID = 0;
        int frameID = 0;
        public int Duration_song_s { get => duration_song_s; }
        public int TimePlaying_song_s { get => timePlaying_song_s; }
        public int SongID { get => songID; }

        //headerPacket
        static headerPacket HeaderPacket = new headerPacket();

        //for threadSend
        const int Max_send_buff_length = 1472;//1472; //534
        byte[] sendBuffer = new byte[Max_send_buff_length];

        //List soundTrack
        List<soundTrack> soundList = new List<soundTrack>();
        public List<soundTrack> SoundList { get => soundList; set => soundList = value; }


        //status to set up: play, pause, stop
        internal int resumeNextPrevious = 0;
        public enum status_enum { PLAY, PAUSE, STOP };
        status_enum status = status_enum.STOP;
        public status_enum Status { get => status; }

        static List<byte[]> aduFrameList = new List<byte[]>();
        static int maxSizeListAdu;
        public List<Socket> listTestClientSocket = new List<Socket>();
        public List<EndPoint> listTestClientEndPoint = new List<EndPoint>();

        public bool launchUDPsocket(List<soundTrack> _soundList, List<client_IPEndPoint> _clientList)
        {
            soundList = _soundList;
            clientList = _clientList;
            maxSizeListAdu = (aduFrameList.Count - 8);
            try
            {
                //socket.Bind(localEndPoint);
                socket = new MulticastUDPServer(IPAddress.Any, localPort);
                socket.Start();
                //Console.WriteLine($"Local socket bind to {localEndPoint}. Waiting for request ...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            //create UDP socket listen from client
            UDPsocketListen();
            //create UDP socket for sending mp3 frame to client
            UDPsocketSend();

            return true;
        }

        public void UDPsocketListen()
        {
            Stopwatch watch_client = new Stopwatch();
            watch_client.Start();

            threadListen = new Thread(() =>
            {
                threadListenFunc(watch_client);
            });

            threadCheckRequest = new Thread(() =>
            {
                threadCheckRequestFunc(watch_client);
            });

            threadCheckRequest.Priority = ThreadPriority.Lowest;
            threadListen.Priority = ThreadPriority.Normal;

            threadListen.Start();
            threadCheckRequest.Start();
        }

        public void UDPsocketSend()
        {
            threadSend = new Thread(() =>
            {
                threadSendFunc();
            });
            threadSend.Priority = ThreadPriority.Highest;
            threadSend.Start();
        }

        public bool controlThreadSend(int _control)
        {
            //_control: 1      2       3      4         5
            //          pause, resume, next, previous, stop
            if ((_control == 1) && (status == status_enum.PLAY))
            {
                status = status_enum.PAUSE;
                //stopWatchSend.Stop();
                //threadSend.Suspend();
                //threadSend.Sleep(Timeout.Infinite);
            }
            else if ((_control == 2) && (status == status_enum.PAUSE))
            {
                status = status_enum.PLAY;
                resumeNextPrevious = 2;
                //stopWatchSend.Start();
                //threadSend.Resume();
            }
            else if (status != status_enum.STOP)
            {
                if (_control == 3) //next
                {
                    resumeNextPrevious = 3;
                    status = status_enum.PLAY;
                }
                else if (_control == 4)
                {
                    resumeNextPrevious = 4;
                    status = status_enum.PLAY;
                }
                else if (_control == 5)
                {
                    status = status_enum.STOP;
                    //threadSend.Abort();
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        private void analyzeRequest(int length, Stopwatch _watchClient)
        {
            if (length >= 8)
            {
                //test request
                /*IPEndPoint receive = (IPEndPoint)receive_IPEndPoint;
                Console.WriteLine("This message lentgh:" + length.ToString() + " was sent from " +
                                            receive.Address.ToString() +
                                            " on their port number " + receive.Port.ToString());*/


                //get id client
                var ID_client_received = Encoding.ASCII.GetString(receive_buffer, 0, 8);

                //check client in List
                for (int i = 0; i < ClientList.Count; i++)
                {
                    if (String.Equals(ID_client_received, ClientList[i].ID_client))
                    {
                        ClientList[i].TimeStamp_ms = _watchClient.ElapsedMilliseconds; //update time request
                        ClientList[i].TimeOut = false;
                        ClientList[i].IPEndPoint_client = receive_IPEndPoint; //update IP, port UDP of client
                    }
                }
            }
        }
        bool first_first = true;
        private void threadListenFunc(Stopwatch _watchClient)
        {
            while (true)
            {
                //client need to send ID, string 4 byte
                int length = 0;
                try
                {
                    length = (int)socket.Receive(ref receive_IPEndPoint, receive_buffer);
                    //length = socket.ReceiveFrom(receive_buffer, ref receive_IPEndPoint);
                    //var result = Encoding.ASCII.GetString(receive_buffer, 0, length);
                    //Console.WriteLine("{0} {1}", receive_IPEndPoint, result);
                    if (first_first)
                    {
                        var result = Encoding.ASCII.GetString(receive_buffer, 0, length);
                        if(result.Contains("3"))
                        {
                            first_first = false;
                            listTestClientEndPoint.Add(receive_IPEndPoint);
                        }
                        //Console.WriteLine("{0} {1}", receive_IPEndPoint, result);
                        //first_first = false;

                        //IPAddress ipaddrtmp = IPAddress.Parse("8.8.8.8"); //14.162.122.48
                        //Console.WriteLine("IP address fixed {0}", ipaddrtmp);
                        //int ipporttmp = ((IPEndPoint)receive_IPEndPoint).Port;

                        //for (int i = 0; i < 500; i++)
                        //{
                        //    EndPoint tmpEndPoint = new IPEndPoint(ipaddrtmp, ipporttmp + i);
                        //    listTestClientEndPoint.Add(tmpEndPoint);
                        //}
                    }
                }
                catch//(Exception ex)
                {
                    //Console.WriteLine(ex);
                    continue;
                }

                analyzeRequest(length, _watchClient);
            }
        }

        private void threadCheckRequestFunc(Stopwatch _watchClient)
        {
            while (true)
            {
                for (int i = 0; i < ClientList.Count; i++)
                {
                    double offsetTime = _watchClient.ElapsedMilliseconds - ClientList[i].TimeStamp_ms;
                    if (offsetTime > 60000) // > 60s
                    {
                        ClientList[i].TimeOut = true;
                    }
                }
                Thread.Sleep(60000); //check every 60s
            }
        }

        private void threadSendFunc()
        {
            status = status_enum.PLAY;
            while (true)
            {
                for (int i = 0; i < soundList.Count; i++)
                {
                    HeaderPacket.IDsong = (byte)(i + 1);
                    HeaderPacket.IDframe = 0;
                    int value_control = 0;
                    byte[] mp3_data;
                    try
                    {
                        mp3_data = File.ReadAllBytes(soundList[i].FilePath);

                        byte[] mp3_buff = File.ReadAllBytes(soundList[i].FilePath).Skip(237).ToArray();
                        //MP3_ADU mp3file = new MP3_ADU(mp3_buff, mp3_buff.Length);
                        //int numFrame = 0;

                        ADU_frame adufile = new ADU_frame(mp3_buff, mp3_buff.Length);
                        int aduNumFrame = 0;

                        //FileStream stream = new FileStream(@"E:\test10.mp3", FileMode.Append);

                        List<byte[]> _aduFrameList = new List<byte[]>();
                        while (true)
                        {
                            byte[] aduframe = adufile.ReadNextADUFrame();
                            if (aduframe != null)
                            {
                                aduNumFrame++;
                                _aduFrameList.Add(aduframe);
                            }
                            else
                            {
                                break;
                            }
                        }

                        aduFrameList = _aduFrameList;

                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        continue;
                    }
                    songID = i;
                    value_control = sendPacketMP3(mp3_data, mp3_data.Length);

                    if (value_control == 3) //next
                    {
                        Thread.Sleep(1000);
                    }
                    else if (value_control == 4) //previuos
                    {
                        if (i > 0)
                        {
                            i -= 2;
                            //continue;
                        }
                        else if (i == 0)
                        {
                            i = soundList.Count - 2;
                        }
                    }
                    else if (value_control == 5) //stop
                    {
                        songID = 0;
                        timePlaying_song_s = 0;
                        duration_song_s = 0;
                        return;
                    }

                }
                if (!loopBack)
                {
                    break;
                }
            }
        }

        private int sendPacketMP3(byte[] mp3_buff, int mp3_buff_length)
        {

            double timePoint, mark_time = 0;

            MP3_frame mp3_reader = new MP3_frame(mp3_buff, mp3_buff_length);
            if (!mp3_reader.IsValidMp3())
            {
                return -1;
            }
            //const double framemp3_time = (double)1152.0 * 1000.0 / 44100.0; //ms
            double framemp3_time = mp3_reader.TimePerFrame_ms;
            //count total Frame of mp3
            duration_song_s = mp3_reader.countFrame() * (int)mp3_reader.TimePerFrame_ms / 1000;

            int orderFrame = 0;

            SocketFlags socketFlag = new SocketFlags();

            //launch timer
            Stopwatch stopWatchSend = new Stopwatch();
            stopWatchSend.Start();

            int tmp;
            while (true)
            {

                byte[] sendADU = packet_udp_frameADU(orderFrame);
                bool endOfFile = false;
                //byte[] sendADU = new byte[200];
                //sendADU[0] = (byte)'h';
                //sendADU[1] = (byte)'i';

                //change status, return value to threadSendFunc
                //value: 1-play-next, 2-play-previous, 3-stop
                tmp = resumeNextPrevious;
                if (status == status_enum.STOP)
                {
                    tmp = 5;
                    sendADU = null;
                }
                else if (resumeNextPrevious > 0)
                {
                    resumeNextPrevious = 0;
                    if (tmp == 2)
                    {
                        stopWatchSend.Start();
                    }
                    else if (tmp > 4)
                    {
                        tmp = 5;
                    }
                    sendADU = null;
                }
                else if (status == status_enum.PAUSE)
                {
                    stopWatchSend.Stop();
                    Thread.Sleep(500);
                    sendADU = null;
                }

                if (sendADU == null)
                {
                    //sendADU = new byte[10];
                    //sendADU[0] = 0xAA;
                    endOfFile = true;
                    break;
                }
                for (int i = 0; i < clientList.Count; i++)
                {
                    if ((!clientList[i].TimeOut) && (clientList[i].On))
                    {
                        for (int j = 0; j < clientList[i].NumSend; j++)
                        {
                            try
                            {
                                socket.SendAsync(clientList[i].IPEndPoint_client, sendADU, 0, sendADU.Length);
                                //Console.WriteLine("$");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                    }
                }

                //send 2 times ~ retrans
                for (int i = 0; i < listTestClientEndPoint.Count; i++)
                {
                    try
                    {
                        socket.SendAsync(listTestClientEndPoint[i], sendADU);
                        //listTestClientSocket[i].BeginSend(sendADU, 0, sendADU.Length, SocketFlags.None, SendCallback, listTestClientSocket[i]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                if (endOfFile) break;

                orderFrame++;

                //Console.WriteLine(orderFrame);
                mark_time = 24 * orderFrame; //point to next time frame
                
                //get current time playing
                timePlaying_song_s = (int)mark_time / 1000; //second
                timePoint = mark_time - stopWatchSend.Elapsed.TotalMilliseconds;
                //if (timePoint < 0) Console.WriteLine(timePoint);
                if (timePoint > 0)
                {
                    Thread.Sleep((int)timePoint);
                }
            }

            //done a song
            timePlaying_song_s = 0;
            duration_song_s = 0;
            stopWatchSend.Stop();
            Thread.Sleep(500);
            return tmp;
        }

        static private int packet_udp_frameMP3(byte[] _send_buff, MP3_frame _mp3_reader)
        {
            //reset packet header
            HeaderPacket.NumOffFrame = 0;
            HeaderPacket.TotalLength = HeaderPacket.Length;
            while (true)
            {
                if (left_frame_not_packet)
                {
                    left_frame_not_packet = false;
                }
                else if (_mp3_reader.ReadNextFrame())
                {

                }
                else
                {
                    break;
                }
                //check space for cmemcpy //(4+4) for numOfFrame, totalLength
                if (_mp3_reader.Frame_size <= (Max_send_buff_length - HeaderPacket.TotalLength))
                {
                    System.Buffer.BlockCopy(_mp3_reader.Mp3_buff, _mp3_reader.Start_frame, _send_buff, HeaderPacket.TotalLength, _mp3_reader.Frame_size);
                    HeaderPacket.TotalLength += (UInt16)(_mp3_reader.Frame_size);
                    HeaderPacket.NumOffFrame++;
                }
                else
                {
                    left_frame_not_packet = true;
                    break;
                }
            }

            //copy header to _send_buff
            HeaderPacket.copyHeaderToBuffer(_send_buff);

            sizeOfPacket = HeaderPacket.TotalLength;

            //increase IDframe
            HeaderPacket.IDframe++;

            return HeaderPacket.NumOffFrame;
        }
        //static int[] orderArray = { 0, 2, 4, 6, 1, 3, 5, 7 };
        static int[] orderArray = { 0, 1, 2, 3, 4, 5, 6, 7 }; // no interleave
        static int orderArrayIndex = 0;
        static int packetIndex = 0;
        static private byte[] packet_udp_frameADU(int _numOfFrame)
        {
            if (_numOfFrame == aduFrameList.Count)
            {
                return null;
            }

            //int iListADU = (_numOfFrame / 8) * 8 + orderArray[orderArrayIndex];
            int iListADU = _numOfFrame;
            int sizeOfADUpacket = aduFrameList[iListADU].Length + 2 + 4 + 4; //2B checksum, 4B id adu frame, 4B idSong
            byte[] tmpADUpacket = new byte[sizeOfADUpacket];
            //copy adu id
            System.Buffer.BlockCopy(BitConverter.GetBytes(iListADU + 1), 0, tmpADUpacket, 2, 4); //start from 1
            //copy song id
            System.Buffer.BlockCopy(BitConverter.GetBytes(songID + 1), 0, tmpADUpacket, 2 + 4, 4);
            //copy adu data
            System.Buffer.BlockCopy(aduFrameList[iListADU], 0, tmpADUpacket, 2 + 4 + 4, aduFrameList[iListADU].Length);
            //checksum
            UInt16 checkSum = caculateChecksum(tmpADUpacket, 2, 4 + 4 + aduFrameList[iListADU].Length);
            //copy checksum
            System.Buffer.BlockCopy(BitConverter.GetBytes(checkSum), 0, tmpADUpacket, 0, 2);

            //
            //orderArrayIndex++;
            //if (orderArrayIndex >= 8)
            //{
            //    orderArrayIndex = 0;
            //    packetIndex++;
            //    if (packetIndex >= aduFrameList.Count / 8)
            //    {
            //        packetIndex = 0;
            //        return null;
            //    }
            //}

            return tmpADUpacket;
        }

        static UInt16 caculateChecksum(byte[] data, int offset, int length)
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
    }

    class client_IPEndPoint
    {
        // biến này về sau sẽ chứa địa chỉ của tiến trình client nào gửi gói tin tới
        EndPoint ipEndPoint_client = new IPEndPoint(IPAddress.Any, 0);

        string id_client;

        public EndPoint IPEndPoint_client { get => ipEndPoint_client; set => ipEndPoint_client = value; }
        public string ID_client { get => id_client; set => id_client = value; }

        double timeStamp_ms = 0;

        bool timeOut = true; //timeOut = true, that mean don't receive request in last 5s, and don't send

        bool on; //change this on app

        int numSend = 1; //multi packet is sent to client to improve UDP loss
        public int NumSend { get => numSend; set => numSend = value; }

        //server just sends to client when timeOut == false and On == true

        public double TimeStamp_ms { get => timeStamp_ms; set => timeStamp_ms = value; }
        public bool TimeOut { get => timeOut; set => timeOut = value; }
        public bool On { get => on; set => on = value; }
    }

    class soundTrack
    {
        string filePath;
        int duration_ms = 0; //duration of a sound Track
        int playingTime_ms = 0; //current time playing of sound track

        public string FilePath { get => filePath; set => filePath = value; }
        public int Duration_ms { get => duration_ms; }
        public int PlayingTime_ms { get => playingTime_ms; }
    }

    class headerPacket
    {
        ////header of UDP packet
        //1-byte: volume, 1-byte: ID_song, 2-byte: totalLength
        //4-byte: ID_frame
        //2-byte: numOfFrame, 2-byte checksum
        //de cho an toan, nen tinh checksum cho header nay

        //total byte in header
        UInt16 length = 14;
        byte volume = 0x00; // max:min 0x00:0xFE
        byte id_song;
        UInt16 totalLength;
        UInt32 id_frame;
        UInt16 numOffFrame;
        UInt16 checkSum;
        UInt16 checkSumData;

        public UInt16 Length { get => length; }

        internal byte IDsong { get => id_song; set => id_song = value; }
        internal ushort TotalLength { get => totalLength; set => totalLength = value; }
        internal uint IDframe { get => id_frame; set => id_frame = value; }
        internal ushort NumOffFrame { get => numOffFrame; set => numOffFrame = value; }

        internal byte Volume
        {
            get { return volume; }
            set
            {
                if (value == 0xFF)
                    volume = 0xFE;
                else
                    volume = value;
            }
        }

        internal void copyHeaderToBuffer(byte[] _buffer)
        {
            _buffer[0] = volume;
            _buffer[1] = id_song;
            byte[] tmp_byte = new byte[4];
            tmp_byte = BitConverter.GetBytes(totalLength);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 2, 2);
            tmp_byte = BitConverter.GetBytes(id_frame);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 4, 4);
            tmp_byte = BitConverter.GetBytes(numOffFrame);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 8, 2);

            //caculate checksum for header and checksum for data
            checkSum = caculateChecksum(_buffer, 0, length - 4); //header
            checkSumData = caculateChecksum(_buffer, length, totalLength - length);

            tmp_byte = BitConverter.GetBytes(checkSum);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 10, 2);
            tmp_byte = BitConverter.GetBytes(checkSumData);
            System.Buffer.BlockCopy(tmp_byte, 0, _buffer, 12, 2);
        }

        static UInt16 caculateChecksum(byte[] data, int offset, int length)
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

    }
}

