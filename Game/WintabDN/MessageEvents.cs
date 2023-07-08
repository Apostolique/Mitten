///////////////////////////////////////////////////////////////////////////////
//
//	PURPOSE
//		Native Windows message handling for WintabDN
//
//	COMMENT
//		The code in this file is based on the example given at:
//		http://msdn.microsoft.com/en-us/magazine/cc163417.aspx
//		by Steven Toub.
//
//	COPYRIGHT
//		Copyright (c) 2010-2020 Wacom Co., Ltd.
//
//		The text and information contained in this file may be freely used,
//		copied, or distributed without compensation or licensing restrictions.
//
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace WintabDN
{
    /// <summary>
    /// Support for registering a Native Windows message with MessageEvents class.
    /// </summary>
    // public class MessageReceivedEventArgs : EventArgs
    // {
    // 	private readonly Message _message;

    // 	/// <summary>
    // 	/// MessageReceivedEventArgs constructor.
    // 	/// </summary>
    // 	/// <param name="message">Native windows message to be registered.</param>
    // 	public MessageReceivedEventArgs(Message message) { _message = message; }

    // 	/// <summary>
    // 	/// Return native Windows message handled by this object.
    // 	/// </summary>
    // 	public Message Message { get { return _message; } }
    // }

    // /// <summary>
    // /// Windows native message handler, to provide support for detecting and
    // /// responding to Wintab messages.
    // /// </summary>
    // public static class MessageEvents
    // {
    // 	private static object _lock = new object();
    // 	private static MessageWindow _window;
    // 	private static IntPtr _windowHandle;
    // 	private static SynchronizationContext _context;

    // 	/// <summary>
    // 	/// MessageEvents delegate.
    // 	/// </summary>
    // 	public static event EventHandler<MessageReceivedEventArgs> PacketMessageReceived;
    // 	public static event EventHandler<MessageReceivedEventArgs> StatusMessageReceived;
    // 	public static event EventHandler<MessageReceivedEventArgs> InfoChgMessageReceived;

    // 	/// <summary>
    // 	/// Registers to receive the specified native Windows message.
    // 	/// </summary>
    // 	/// <param name="message">Native Windows message to watch for.</param>
    // 	public static void WatchMessage(EWintabEventMessage message)
    // 	{
    // 		EnsureInitialized();
    // 		switch (message)
    // 		{
    // 			case EWintabEventMessage.WT_PACKET:
    // 			case EWintabEventMessage.WT_PACKETEXT:
    // 			case EWintabEventMessage.WT_CSRCHANGE:
    // 			{
    // 				_window.RegisterPacketEventForMessage((int)message, true);
    // 				break;
    // 			}

    // 			case EWintabEventMessage.WT_CTXOPEN:
    // 			case EWintabEventMessage.WT_CTXCLOSE:
    // 			case EWintabEventMessage.WT_CTXUPDATE:
    // 			case EWintabEventMessage.WT_CTXOVERLAP:
    // 			case EWintabEventMessage.WT_PROXIMITY:
    // 			{
    // 				_window.RegisterStatusEventForMessage((int)message, true);
    // 				break;
    // 			}

    // 			case EWintabEventMessage.WT_INFOCHANGE:
    // 			{
    // 				_window.RegisterInfoChgEventForMessage((int)message, true);
    // 				break;
    // 			}

    // 			default:
    // 			{
    // 				throw new Exception("Bad packet message");
    // 			}
    // 		}
    // 	}

    // 	/// <summary>
    // 	/// Registers to receive the specified native Windows message.
    // 	/// </summary>
    // 	/// <param name="message">Native Windows message to watch for.</param>
    // 	public static void UnWatchMessage(EWintabEventMessage message)
    // 	{
    // 		EnsureInitialized();
    // 		switch (message)
    // 		{
    // 			case EWintabEventMessage.WT_PACKET:
    // 			case EWintabEventMessage.WT_PACKETEXT:
    // 			case EWintabEventMessage.WT_CSRCHANGE:
    // 			{
    // 				_window.RegisterPacketEventForMessage((int)message, false);
    // 				break;
    // 			}

    // 			case EWintabEventMessage.WT_CTXOPEN:
    // 			case EWintabEventMessage.WT_CTXCLOSE:
    // 			case EWintabEventMessage.WT_CTXUPDATE:
    // 			case EWintabEventMessage.WT_CTXOVERLAP:
    // 			case EWintabEventMessage.WT_PROXIMITY:
    // 			{
    // 				_window.RegisterStatusEventForMessage((int)message, false);
    // 				break;
    // 			}

    // 			case EWintabEventMessage.WT_INFOCHANGE:
    // 			{
    // 				_window.RegisterInfoChgEventForMessage((int)message, false);
    // 				break;
    // 			}

    // 			default:
    // 			{
    // 				throw new Exception("Bad packet message");
    // 			}
    // 		}
    // 	}

    // 	/// <summary>
    // 	/// Returns the MessageEvents native Windows handle.
    // 	/// </summary>
    // 	public static IntPtr WindowHandle
    // 	{
    // 		get
    // 		{
    // 			EnsureInitialized();
    // 			return _windowHandle;
    // 		}
    // 	}

    // 	///////////////////////////////////////////////////////////////////////

    // 	private static void EnsureInitialized()
    // 	{
    // 		lock (_lock)
    // 		{
    // 			if (_window == null)
    // 			{
    // 				_context = AsyncOperationManager.SynchronizationContext;
    // 				using (ManualResetEvent mre = new ManualResetEvent(false))
    // 				{
    // 					Thread t = new Thread((ThreadStart)delegate
    // 					{
    // 						_window = new MessageWindow();
    // 						_windowHandle = _window.Handle;
    // 						mre.Set();
    // 						Application.Run();
    // 					});
    // 					t.Name = "MessageEvents message loop";
    // 					t.IsBackground = true;
    // 					t.Start();

    // 					mre.WaitOne();
    // 				}
    // 			}
    // 		}
    // 	}

    // 	///////////////////////////////////////////////////////////////////////

    // 	private class MessageWindow : Form
    // 	{
    // 		private ReaderWriterLock _lock = new ReaderWriterLock();
    // 		private Dictionary<int, bool> _messagePacketSet = new Dictionary<int, bool>();
    // 		private Dictionary<int, bool> _messageStatusSet = new Dictionary<int, bool>();
    // 		private Dictionary<int, bool> _messageInfoChgSet = new Dictionary<int, bool>();

    // 		public void RegisterPacketEventForMessage(int messageID, bool state)
    // 		{
    // 			_lock.AcquireWriterLock(Timeout.Infinite);
    // 			_messagePacketSet[messageID] = state;
    // 			_lock.ReleaseWriterLock();
    // 		}

    // 		public void RegisterStatusEventForMessage(int messageID, bool state)
    // 		{
    // 			_lock.AcquireWriterLock(Timeout.Infinite);
    // 			_messageStatusSet[messageID] = state;
    // 			_lock.ReleaseWriterLock();
    // 		}
    // 		public void RegisterInfoChgEventForMessage(int messageID, bool state)
    // 		{
    // 			_lock.AcquireWriterLock(Timeout.Infinite);
    // 			_messageInfoChgSet[messageID] = state;
    // 			_lock.ReleaseWriterLock();
    // 		}

    // 		protected override void WndProc(ref Message msg)
    // 		{
    // 			_lock.AcquireReaderLock(Timeout.Infinite);
    // 			bool handlePacketMessage = _messagePacketSet.ContainsKey(msg.Msg);
    // 			bool handleStatusMessage = _messageStatusSet.ContainsKey(msg.Msg);
    // 			bool handleInfoChgMessage = _messageInfoChgSet.ContainsKey(msg.Msg);
    // 			_lock.ReleaseReaderLock();

    // 			if (handlePacketMessage)
    // 			{
    // 				MessageEvents._context.Post(delegate (object state)
    // 				{
    // 					EventHandler<MessageReceivedEventArgs> handler = MessageEvents.PacketMessageReceived;
    // 					if (handler != null)
    // 					{
    // 						handler(null, new MessageReceivedEventArgs((Message)state));
    // 					}
    // 				}, msg);
    // 			}

    // 			if (handleStatusMessage)
    // 			{
    // 				MessageEvents._context.Post(delegate (object state)
    // 				{
    // 					EventHandler<MessageReceivedEventArgs> handler = MessageEvents.StatusMessageReceived;
    // 					if (handler != null)
    // 					{
    // 						handler(null, new MessageReceivedEventArgs((Message)state));
    // 					}
    // 				}, msg);
    // 			}

    // 			if (handleInfoChgMessage)
    // 			{
    // 				MessageEvents._context.Post(delegate (object state)
    // 				{
    // 					EventHandler<MessageReceivedEventArgs> handler = MessageEvents.InfoChgMessageReceived;
    // 					if (handler != null)
    // 					{
    // 						handler(null, new MessageReceivedEventArgs((Message)state));
    // 					}
    // 				}, msg);
    // 			}

    // 			base.WndProc(ref msg);
    // 		}
    // 	}
    // }
}
