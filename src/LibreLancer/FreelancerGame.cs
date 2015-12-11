﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using LibreLancer.GameData;
using LibreLancer.Media;
namespace LibreLancer
{
	public class FreelancerGame : GameWindow, IUIThread
    {
		public FreelancerIni GameIni;
		public FreelancerData GameData;
		public AudioDevice Audio;
		public MusicPlayer Music;
		public ResourceCache ResourceCache;
		ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
		int uithread;
		GameState currentState;
		public Viewport Viewport {
			get {
				return new Viewport (ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
			}
		}
		public FreelancerGame(GameConfig config) : base
		(1024, 768, new GraphicsMode(new ColorFormat(32),
			32), 
            "LibreLancer", GameWindowFlags.Default, 
            DisplayDevice.Default, 3, 2, 
            GraphicsContextFlags.ForwardCompatible)
        {
			//Setup
			uithread = Thread.CurrentThread.ManagedThreadId;
			FLLog.Info("Platform", Platform.RunningOS.ToString());
			VFS.Init(config.FreelancerPath);
			GameIni = new FreelancerIni ();
			GameData = new FreelancerData (GameIni);
			//Cache
			ResourceCache = new ResourceCache();
			//Init Audio
			FLLog.Info("Audio", "Initialising Audio");
			Audio = new AudioDevice();
			Music = new MusicPlayer (Audio);
			//Load data
			FLLog.Info("Game", "Loading game data");
			new Thread (() => {
				GameData.LoadData();
				FLLog.Info("Game", "Finished loading game data");
				QueueUIThread(Switch);
			}).Start ();

        }
		public void QueueUIThread(Action work)
		{
			actions.Enqueue(work);
		}
		public void EnsureUIThread (Action work)
		{
			if (Thread.CurrentThread.ManagedThreadId == uithread)
				work ();
			else {
				bool done = false;
				actions.Enqueue (() => {
					work();
					done = true;
				});
				while (!done)
					Thread.Sleep (1);
			}
		}
		void Switch()
		{
			currentState = new DemoSystemView (this);
		}
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GL.ClearColor(Color4.Black);
			GL.Enable (EnableCap.DepthTest);
			GL.Enable (EnableCap.CullFace);
			GL.CullFace (CullFaceMode.Back);
			var vp = new ViewportManager ();
			vp.Push (0, 0, 1024, 768);
        }

		protected override void OnClosing (System.ComponentModel.CancelEventArgs e)
		{
			Music.Stop ();
			Audio.Dispose ();
			base.OnClosing (e);
		}
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			Action work;
			if (actions.TryDequeue (out work))
				work ();
			if (currentState != null)
				currentState.Update (TimeSpan.FromSeconds (e.Time));
			FLLog.Debug ("ResourceCache", "Mesh Count: " + ResourceCache.MeshCount);
			base.OnUpdateFrame (e);
		}
        protected override void OnRenderFrame(FrameEventArgs e)
        {
			Title = string.Format ("LibreLancer: {0}fps", RenderFrequency);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			if (currentState != null)
				currentState.Draw (TimeSpan.FromSeconds (e.Time));
            SwapBuffers();
            base.OnRenderFrame(e);
        }
    }
}
