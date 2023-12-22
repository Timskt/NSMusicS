﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf.Transitions;
using System.Collections.ObjectModel;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using TransitionEffect = MaterialDesignThemes.Wpf.Transitions.TransitionEffect;
using ImageMagick;
using NSMusicS.Dao_UserControl.SingerImage_Info;
using static NSMusicS.UserControlLibrary.MusicPlayer_Main.UserControls.UserControl_Animation.ViewModel.MainViewModel_Animation_1;
using System.Windows.Media.Media3D;
using System.Windows;
using System.Windows.Media.Imaging;
using NSMusicS.Models.Song_List_Of_AlbumList_Infos;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using NSMusicS.UserControlLibrary.MusicPlayer_Main.MusicPlayer_Model_Control_Albums.ViewModel_Assembly_Album_Show;
using System.Threading;
using static NSMusicS.UserControlLibrary.MusicPlayer_Main.MusicPlayer_Model_Control_Albums.ViewModel_Assembly_Album_Show.ViewModel_Assembly_Album_Class;

namespace NSMusicS.UserControlLibrary.MusicPlayer_Main.MusicPlayer_Model_Control_Albums.ViewModel_Assembly_Album_Show
{
    public class ViewModel_Assembly_Album_Class : ViewModelBase
    {
        public class Album_Info
        {
            public int Album_No { get; set; }
            public string Album_Name { get; set; }
            public string Album_Explain { get; set; }
            public Uri Album_Image_Uri { get; set; }
            public ImageBrush Album_Image { get; set; }
            public TransitionEffect Effact { get; set; }

            public double Width { get; set; }
            public double Height { get; set; }
            public Thickness Margin { get; set; }
        }
        public int Num_Album_Infos { get; set; }//检测是否已完成RelayCommand

        public ViewModel_Assembly_Album_Class()
        {
            kinds = new List<TransitionEffectKind>
            {
                TransitionEffectKind.ExpandIn,//渐显和展开
                TransitionEffectKind.FadeIn,//逐渐淡入，从完全透明到完全可见
                TransitionEffectKind.SlideInFromLeft,//沿着水平方向从左边滑入
                TransitionEffectKind.SlideInFromTop,
                TransitionEffectKind.SlideInFromRight,
                TransitionEffectKind.SlideInFromBottom
            };

            Album_Infos = new ObservableCollection<Album_Info>();
            Num_Album_Infos = 0;

            /// 一次性全部刷新（一致性）
            RefCommand = new RelayCommand(async () =>
            {
                Album_Info_Class album_Info_Class = Album_Info_Class.Retuen_This();
                for (int i = 0; i < album_Info_Class.Album_Image_Uris.Count; i++)
                {
                    Album_Infos.Add(new Album_Info()
                    {
                        Album_No = i,
                        Album_Name = album_Info_Class.Album_Names[i],
                        Album_Image = new ImageBrush(new BitmapImage(album_Info_Class.Album_Image_Uris[i])),
                        Album_Explain = album_Info_Class.Album_Explain[i],
                        Width = 140,
                        Height = 140,
                        Margin = new Thickness(10, 2, 10, 2),
                        Effact = new TransitionEffect()
                        {
                            Kind = kinds[2],//使用渐变效果,从左边滑入
                            Duration = new TimeSpan(0, 0, 0, 0, 40)
                        }
                    });
                    await Task.Delay(40);//单个平滑过渡
                    Num_Album_Infos++;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
                }
            });
            /// 滚动条多次异步刷新（一致性）
            RefCommand_Async = new RelayCommand(async () =>
            {
                Album_Info_Class album_Info_Class = Album_Info_Class.Retuen_This();

                for (int i = album_Info_Class.Start_Index; i <= album_Info_Class.End_Index; i++)
                {
                    if (i >= album_Info_Class.Album_Names.Count || i >= album_Info_Class.Album_Image_Uris.Count)
                        break;

                    if (album_Info_Class.Album_Names[i] != null)
                    {
                        var existingAlbum = Album_Infos.FirstOrDefault(
                            item => item.Album_Name.Equals(album_Info_Class.Album_Names[i])
                            );
                        if (existingAlbum == null)
                        {
                            lock (Album_Infos)
                            {
                                var albumInfo = new Album_Info()
                                {
                                    Album_No = i,
                                    Album_Name = album_Info_Class.Album_Names[i],
                                    Album_Image_Uri = album_Info_Class.Album_Image_Uris[i],
                                    Album_Image = new ImageBrush(new BitmapImage(album_Info_Class.Album_Image_Uris[i])),
                                    Album_Explain = album_Info_Class.Album_Explain[i],
                                    Width = 140,
                                    Height = 140,
                                    Margin = new Thickness(10, 2, 10, 2),
                                    Effact = new TransitionEffect()
                                    {
                                        Kind = kinds[new Random().Next(2, 6)],
                                        Duration = new TimeSpan(0, 0, 0, 0, 200)
                                    }
                                };
                                // 添加到队列中
                                AddToQueue(albumInfo);
                            }
                        }
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
                }
            });
        }

        public RelayCommand RefCommand { get; set; }
        public RelayCommand RefCommand_Async { get; set; }

        public List<TransitionEffectKind> kinds;
        private ObservableCollection<Album_Info> album_Infos;
        public ObservableCollection<Album_Info> Album_Infos
        {
            get { return album_Infos; }
            set { album_Infos = value; RaisePropertyChanged(); }
        }
        //保证数据一致性 + 动画过渡
        private readonly Queue<Album_Info> AlbumInfoQueue = new Queue<Album_Info>();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        public void AddToQueue(Album_Info albumInfo)
        {
            AlbumInfoQueue.Enqueue(albumInfo);
            ProcessQueue();
        }
        private async Task ProcessQueue()
        {
            await semaphore.WaitAsync();

            try
            {
                while (AlbumInfoQueue.Count > 0)
                {
                    var albumInfo = AlbumInfoQueue.Dequeue();

                    var existingAlbum = Album_Infos.FirstOrDefault(
                        item => item.Album_Name.Equals(albumInfo.Album_Name)
                        );
                    if (existingAlbum == null)
                    {
                        Album_Infos.Add(albumInfo);
                        await Task.Delay(40); // 单个平滑过渡
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }




        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        public static ViewModel_Assembly_Album_Class temp { get; set; }
        public static ViewModel_Assembly_Album_Class Retuen_This()
        {
            temp = Return_This_Album_Performer_List_Infos();
            return temp;
        }
        private static ViewModel_Assembly_Album_Class Return_This_Album_Performer_List_Infos()
        {
            if (temp == null)
                temp = new ViewModel_Assembly_Album_Class();
            return temp;
        }

    }
}
