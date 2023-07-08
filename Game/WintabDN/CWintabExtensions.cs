///////////////////////////////////////////////////////////////////////////////
//
//	PURPOSE
//		Wintab extensions access for WintabDN
//
//	COPYRIGHT
//		Copyright (c) 2013-2020 Wacom Co., Ltd.
//
//		The text and information contained in this file may be freely used,
//		copied, or distributed without compensation or licensing restrictions.
//
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WintabDN
{
    /// <summary>
    /// Globals used for Wintab extensions.
    /// </summary>
    public class WTExtensionsGlobal
    {
        /// <summary>
        /// Maximum size of data buffer used in WTExtensionProperty.
        /// </summary>
        public const int WTExtensionPropertyMaxDataBytes = 32;
        public const int WTExtensionPropertyImageMaxDataBytes = 4096;
    }

    /// <summary>
    /// Tag values used to get extension masks in GetWTExtensionMask
    /// </summary>
    public enum EWTXExtensionTag
    {
        // enums 0 - 5 are deprecated
        /// <summary>
        /// Touch Strip extension mask tag
        /// </summary>
        WTX_TOUCHSTRIP = 6,

        /// <summary>
        /// Touch Ring extension mask tag
        /// </summary>
        WTX_TOUCHRING = 7,

        /// <summary>
        /// Express Key extension mask tag
        /// </summary>
        WTX_EXPKEYS2 = 8
    }

    /// <summary>
    /// Index values used for WTI extensions.
    /// For more information, see Wintab 1.4.
    /// </summary>
    public enum EWTIExtensionIndex
    {
        /// <summary>
        /// Get a unique, null-terminated string describing the extension.
        /// </summary>
        EXT_NAME = 1,

        /// <summary>
        /// Get a unique identifier for the extension.
        /// </summary>
        EXT_TAG = 2,

        /// <summary>
        /// Get a mask that can be bitwise OR'ed with WTPKT-type variables to select the extension.
        /// </summary>
        EXT_MASK = 3,

        /// <summary>
        /// Get an array of two UINTs specifying the extension's size within a packet (in bytes). The first is for absolute mode; the second is for relative mode.
        /// </summary>
        EXT_SIZE = 4,

        /// <summary>
        /// Get an array of axis descriptions, as needed for the extension.
        /// </summary>
        EXT_AXES = 5,

        /// <summary>
        /// get the current global default data, as needed for the extension.
        /// </summary>
        EXT_DEFAULT = 6,

        /// <summary>
        /// Get the current default digitizing context-specific data, as needed for the extension.
        /// </summary>
        EXT_DEFCONTEXT = 7,

        /// <summary>
        /// Get the current default system context-specific data, as needed for the extension.
        /// </summary>
        EXT_DEFSYSCTX = 8,

        /// <summary>
        /// Get a byte array of the current default cursor-specific data, as need for the extension.
        /// </summary>
        EXT_CURSORS = 9,

        /// <summary>
        /// Allow 100 cursors
        /// </summary>
        EXT_DEVICES = 110,

        /// <summary>
        /// Allow 100 devices
        /// </summary>
        EXT_MAX = 210   // Allow 100 devices
    }

    /// <summary>
    /// Tablet property values used with WTExtGet and WTExtSet
    /// </summary>
    public enum EWTExtensionTabletProperty
    {
        /// <summary>
        /// number of physical controls on tablet
        /// </summary>
        TABLET_PROPERTY_CONTROLCOUNT = 0,

        /// <summary>
        /// number of functions of control
        /// </summary>
        TABLET_PROPERTY_FUNCCOUNT = 1,

        /// <summary>
        /// control/mode is available for override
        /// </summary>
        TABLET_PROPERTY_AVAILABLE = 2,

        /// <summary>
        /// minimum value of control function
        /// </summary>
        TABLET_PROPERTY_MIN = 3,

        /// <summary>
        /// maximum value of control function
        /// </summary>
        TABLET_PROPERTY_MAX = 4,

        /// <summary>
        /// Indicates control should be overriden
        /// </summary>
        TABLET_PROPERTY_OVERRIDE = 5,

        /// <summary>
        ///  UTF8 encoded displayable name when control is overridden
        /// </summary>
        TABLET_PROPERTY_OVERRIDE_NAME = 6,

        /// <summary>
        /// PNG icon image to shown when control is overriden (supported only tablets with display OLEDs; eg: Intuos4)
        /// </summary>
        TABLET_PROPERTY_OVERRIDE_ICON = 7,

        /// <summary>
        /// Pixel width of icon display
        /// </summary>
        TABLET_PROPERTY_ICON_WIDTH = 8,

        /// <summary>
        /// Pixel height of icon display
        /// </summary>
        TABLET_PROPERTY_ICON_HEIGHT = 9,

        /// <summary>
        /// Pixel format of icon display (see TABLET_ICON_FMT_*)
        /// </summary>
        TABLET_PROPERTY_ICON_FORMAT = 10,

        /// <summary>
        /// Physical location of control (see TABLET_LOC_*)
        /// </summary>
        TABLET_PROPERTY_LOCATION = 11
    }

    /// <summary>
    /// Tablet Icon values used with WTExtGet and WTExtSet
    /// </summary>
    public enum EWTExtensionIconProperty
    {
        TABLET_ICON_FMT_NONE = 0,          // No display
        TABLET_ICON_FMT_4BPP_GRAY = 1      // 4bpp grayscale
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WTExtensionPropertyBase
    {
        /// <summary>
        /// Structure version (reserved: always 0 for now)
        /// </summary>
        public byte version;

        /// <summary>
        /// 0-based index for tablet
        /// </summary>
        public byte tabletIndex;

        /// <summary>
        /// 0-based index for control
        /// </summary>
        public byte controlIndex;

        /// <summary>
        /// 0-based index for control's sub-function
        /// </summary>
        public byte functionIndex;

        /// <summary>
        /// ID of property being set (see EWTExtensionTabletProperty)
        /// </summary>
        public UInt16 propertyID;

        /// <summary>
        /// Alignment padding (reserved)
        /// </summary>
        public UInt16 reserved;

        /// <summary>
        /// Number of bytes in data[] buffer
        /// </summary>
        public UInt32 dataSize;
    }

    /// <summary>
    /// Structure for reading/writing non-image Wintab extension data. (Wintab 1.4)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WTExtensionProperty
    {
        public WTExtensionPropertyBase extBase;

        /// <summary>
        /// Non-image data being written/read through the extensions API.
        /// A small buffer is sufficient.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = WTExtensionsGlobal.WTExtensionPropertyMaxDataBytes)]
        public byte[] data;
    }

    /// <summary>
    /// Structure read/writing image Wintab extension data. (Wintab 1.4)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WTExtensionImageProperty
    {
        public WTExtensionPropertyBase extBase;

        /// <summary>
        /// Image data being written through the extensions API.
        /// A large buffer is needed.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = WTExtensionsGlobal.WTExtensionPropertyImageMaxDataBytes)]
        public byte[] data;
    }

    /// <summary>
    /// API for Wintab extensions functionality (Wintab 1.4).
    ///
    /// Wintab Extensions provides support for overriding tablet control functionality with
    /// functionality provided by an application. The tablet controls that can be
    /// overriden with extensions are: Express Keys, Touch Rings and Touch Strips.
    ///
    /// For example, an application can respond to an Express Key button press by
    /// defining what action should occur within that application when the button is pressed.
    /// Similarly, an application can define actions for Touch Ring and Touch Strip button
    /// modes, and respond to user swipes on those controls to provide customized behavior.
    /// </summary>
    public class CWintabExtensions
    {
        /// <summary>
        /// Return the extension mask for the given tag.
        /// </summary>
        /// <param name="tag_I">type of extension being searched for</param>
        /// <returns>0xFFFFFFFF on error</returns>
        public static UInt32 GetWTExtensionMask(EWTXExtensionTag tag_I)
        {
            UInt32 extMask = 0;
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(extMask);

            try
            {
                UInt32 extIndex = FindWTExtensionIndex(tag_I);

                // Supported if extIndex != -1
                if (extIndex != 0xFFFFFFFF)
                {
                    int size = (int)CWintabFuncs.WTInfoA(
                        (uint)EWTICategoryIndex.WTI_EXTENSIONS + (uint)extIndex,
                        (uint)EWTIExtensionIndex.EXT_MASK, buf);

                    extMask = (UInt32)CMemUtils.MarshalUnmanagedBuf<UInt32>(buf, size);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED GetWTExtensionMask: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return extMask;
        }

        /// <summary>
        /// Returns extension index tag for given tag, if possible.
        /// </summary>
        /// <param name="tag_I">type of extension being searched for</param>
        /// <returns>0xFFFFFFFF on error</returns>
        public static UInt32 FindWTExtensionIndex(EWTXExtensionTag tag_I)
        {
            UInt32 thisTag = 0;
            UInt32 extIndex = 0xFFFFFFFF;
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(thisTag);

            for (Int32 loopIdx = 0, size = -1; size != 0; loopIdx++)
            {
                size = (int)CWintabFuncs.WTInfoA(
                    (uint)EWTICategoryIndex.WTI_EXTENSIONS + (UInt32)loopIdx,
                    (uint)EWTIExtensionIndex.EXT_TAG, buf);

                if (size > 0)
                {
                    thisTag = CMemUtils.MarshalUnmanagedBuf<UInt32>(buf, size);

                    if ((EWTXExtensionTag)thisTag == tag_I)
                    {
                        extIndex = (UInt32)loopIdx;
                        break;
                    }
                }
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return extIndex;
        }

        /// <summary>
        /// Get a property value from an extension.
        /// </summary>
        /// <param name="context_I">Wintab context</param>
        /// <param name="extTagIndex_I">extension index tag</param>
        /// <param name="tabletIndex_I">tablet index</param>
        /// <param name="controlIndex_I">control index on the tablet</param>
        /// <param name="functionIndex_I">function index on the control</param>
        /// <param name="propertyID_I">ID of the property requested</param>
        /// <param name="result_O">value of the property requested</param>
        /// <returns>true if property obtained</returns>
        public static bool ControlPropertyGet(
            HCTX context_I,
            byte extTagIndex_I,
            byte tabletIndex_I,
            byte controlIndex_I,
            byte functionIndex_I,
            ushort propertyID_I,
            ref UInt32 result_O
            )
        {
            bool retStatus = false;
            WTExtensionProperty extProperty = new WTExtensionProperty();
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(extProperty);

            extProperty.extBase.version = 0;
            extProperty.extBase.tabletIndex = tabletIndex_I;
            extProperty.extBase.controlIndex = controlIndex_I;
            extProperty.extBase.functionIndex = functionIndex_I;
            extProperty.extBase.propertyID = propertyID_I;
            extProperty.extBase.reserved = 0;
            extProperty.extBase.dataSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(result_O);

            Marshal.StructureToPtr(extProperty, buf, false);

            try
            {
                bool status = CWintabFuncs.WTExtGet((UInt32)context_I, (UInt32)extTagIndex_I, buf);

                if (status)
                {
                    WTExtensionProperty retProp = (WTExtensionProperty)Marshal.PtrToStructure(buf, typeof(WTExtensionProperty));
                    result_O = retProp.data[0];
                    retStatus = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED ControlPropertyGet: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return retStatus;
        }

        /// <summary>
        /// Sets an extension control property value.
        /// </summary>
        /// <param name="context_I">wintab context</param>
        /// <param name="extTagIndex_I">which extension tag we're setting</param>
        /// <param name="tabletIndex_I">index of the tablet being set</param>
        /// <param name="controlIndex_I">the index of the control being set</param>
        /// <param name="functionIndex_I">the index of the control function being set</param>
        /// <param name="propertyID_I">ID of the property being set</param>
        /// <param name="value_I">value of the property being set</param>
        /// <returns>true if successful</returns>
        public static bool ControlPropertySet(
            HCTX context_I,
            byte extTagIndex_I,
            byte tabletIndex_I,
            byte controlIndex_I,
            byte functionIndex_I,
            ushort propertyID_I,
            UInt32 value_I
        )
        {
            bool retStatus = false;
            WTExtensionProperty extProperty = new WTExtensionProperty();
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(extProperty);

            try
            {
                byte[] valueBytes = BitConverter.GetBytes(value_I);

                extProperty.extBase.version = 0;
                extProperty.extBase.tabletIndex = tabletIndex_I;
                extProperty.extBase.controlIndex = controlIndex_I;
                extProperty.extBase.functionIndex = functionIndex_I;
                extProperty.extBase.propertyID = propertyID_I;
                extProperty.extBase.reserved = 0;
                extProperty.extBase.dataSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(value_I);
                extProperty.data = new byte[WTExtensionsGlobal.WTExtensionPropertyMaxDataBytes];

                // Send input value as an array of bytes.
                System.Buffer.BlockCopy(valueBytes, 0, extProperty.data, 0, (int)extProperty.extBase.dataSize);

                Marshal.StructureToPtr(extProperty, buf, false);

                retStatus = CWintabFuncs.WTExtSet((UInt32)context_I, (UInt32)extTagIndex_I, buf);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return retStatus;
        }

        /// <summary>
        /// Sets an extension control property string.
        /// </summary>
        /// <param name="context_I">wintab context</param>
        /// <param name="extTagIndex_I">which extension tag we're setting</param>
        /// <param name="tabletIndex_I">index of the tablet being set</param>
        /// <param name="controlIndex_I">the index of the control being set</param>
        /// <param name="functionIndex_I">the index of the control function being set</param>
        /// <param name="propertyID_I">ID of the property being set</param>
        /// <param name="value_I">value of the property being set (a string)</param>
        /// <returns>true if successful</returns>
        public static bool ControlPropertySet(
            HCTX context_I,
            byte extTagIndex_I,
            byte tabletIndex_I,
            byte controlIndex_I,
            byte functionIndex_I,
            ushort propertyID_I,
            String value_I
            )
        {
            bool retStatus = false;
            WTExtensionProperty extProperty = new WTExtensionProperty();
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(extProperty);

            try
            {
                // Convert unicode string value_I to UTF8-encoded bytes
                byte[] utf8Bytes = System.Text.Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(value_I));

                extProperty.extBase.version = 0;
                extProperty.extBase.tabletIndex = tabletIndex_I;
                extProperty.extBase.controlIndex = controlIndex_I;
                extProperty.extBase.functionIndex = functionIndex_I;
                extProperty.extBase.propertyID = propertyID_I;
                extProperty.extBase.reserved = 0;
                extProperty.extBase.dataSize = (uint)utf8Bytes.Length;
                extProperty.data = new byte[WTExtensionsGlobal.WTExtensionPropertyMaxDataBytes];

                // Send input value as an array of UTF8-encoded bytes.
                System.Buffer.BlockCopy(utf8Bytes, 0, extProperty.data, 0, (int)extProperty.extBase.dataSize);

                Marshal.StructureToPtr(extProperty, buf, false);

                retStatus = CWintabFuncs.WTExtSet((UInt32)context_I, (UInt32)extTagIndex_I, buf);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return retStatus;
        }

        /// <summary>
        /// Sets an extension control property image (if supported by tablet).
        /// </summary>
        /// <param name="context_I">wintab context</param>
        /// <param name="extTagIndex_I">which extension tag we're setting</param>
        /// <param name="tabletIndex_I">index of the tablet being set</param>
        /// <param name="controlIndex_I">the index of the control being set</param>
        /// <param name="functionIndex_I">the index of the control function being set</param>
        /// <param name="propertyID_I">ID of the property being set</param>
        /// <param name="value_I">value of the property being set (a string)</param>
        /// <returns>true if successful</returns>
        // public static bool ControlPropertySetImage(
        // 	HCTX context_I,
        // 	byte extTagIndex_I,
        // 	byte tabletIndex_I,
        // 	byte controlIndex_I,
        // 	byte functionIndex_I,
        // 	ushort propertyID_I,
        // 	String imageFilePath_I
        // 	)
        // {
        // 	bool retStatus = false;
        // 	WTExtensionImageProperty extProperty = new WTExtensionImageProperty();
        // 	IntPtr buf = CMemUtils.AllocUnmanagedBuf(extProperty);

        // 	try
        // 	{
        // 		byte[] imageBytes = null;
        // 		System.Drawing.Image newImage = Image.FromFile(imageFilePath_I);

        // 		if (newImage == null)
        // 		{
        // 			Console.WriteLine("Oops - couldn't find/read image: " + imageFilePath_I);
        // 			return false;
        // 		}

        // 		using (MemoryStream ms = new MemoryStream())
        // 		{
        // 			newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        // 			imageBytes = ms.ToArray();
        // 		}

        // 		extProperty.extBase.version = 0;
        // 		extProperty.extBase.tabletIndex = tabletIndex_I;
        // 		extProperty.extBase.controlIndex = controlIndex_I;
        // 		extProperty.extBase.functionIndex = functionIndex_I;
        // 		extProperty.extBase.propertyID = propertyID_I;
        // 		extProperty.extBase.reserved = 0;
        // 		extProperty.extBase.dataSize = (uint)imageBytes.Length;
        // 		extProperty.data = new byte[WTExtensionsGlobal.WTExtensionPropertyImageMaxDataBytes];

        // 		// Send image as an array of bytes.
        // 		System.Buffer.BlockCopy(imageBytes, 0, extProperty.data, 0, (int)extProperty.extBase.dataSize);

        // 		Marshal.StructureToPtr(extProperty, buf, false);

        // 		retStatus = CWintabFuncs.WTExtSet((UInt32)context_I, (UInt32)extTagIndex_I, buf);
        // 	}
        // 	catch (Exception ex)
        // 	{
        // 		Console.WriteLine(ex.ToString());
        // 	}

        // 	CMemUtils.FreeUnmanagedBuf(buf);

        // 	return retStatus;
        // }

        /// <summary>
        /// Set tablet OLED display property.
        /// </summary>
        /// <param name="context_I">wintab context</param>
        /// <param name="extTagIndex_I">which extension tag we're setting</param>
        /// <param name="tabletIndex_I">index of the tablet being set</param>
        /// <param name="controlIndex_I">the index of the control being set</param>
        /// <param name="functionIndex_I">the index of the control function being set</param>
        /// <param name="imageFilePath_I">path to PNG image file</param>
        /// <returns>true if successful and tablet supports property</returns>
        // public static bool SetDisplayProperty(
        // 	CWintabContext context_I,
        // 	EWTXExtensionTag extTagIndex_I,
        // 	UInt32 tabletIndex_I,
        // 	UInt32 controlIndex_I,
        // 	UInt32 functionIndex_I,
        // 	String imageFilePath_I)
        // {
        // 	UInt32 iconFmt = 0;

        // 	// Bail out if image file not found.
        // 	if (imageFilePath_I == "" ||
        // 		 !System.IO.File.Exists(imageFilePath_I))
        // 	{
        // 		return false;
        // 	}

        // 	try
        // 	{
        // 		if (!CWintabExtensions.ControlPropertyGet(
        // 			context_I.HCtx,
        // 			(byte)extTagIndex_I,
        // 			(byte)tabletIndex_I,
        // 			(byte)controlIndex_I,
        // 			(byte)functionIndex_I,
        // 			(ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_ICON_FORMAT,
        // 			ref iconFmt))
        // 		{ throw new Exception("Oops - Failed ControlPropertyGet for TABLET_PROPERTY_ICON_FORMAT"); }

        // 		if ((EWTExtensionIconProperty)iconFmt != EWTExtensionIconProperty.TABLET_ICON_FMT_NONE)
        // 		{
        // 			// Get the width and height of the display icon.
        // 			UInt32 iconWidth = 0;
        // 			UInt32 iconHeight = 0;

        // 			if (!CWintabExtensions.ControlPropertyGet(
        // 				context_I.HCtx,
        // 				(byte)extTagIndex_I,
        // 				(byte)tabletIndex_I,
        // 				(byte)controlIndex_I,
        // 				(byte)functionIndex_I,
        // 				(ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_ICON_WIDTH,
        // 				ref iconWidth))
        // 			{ throw new Exception("Oops - Failed ControlPropertyGet for TABLET_PROPERTY_ICON_WIDTH"); }

        // 			if (!CWintabExtensions.ControlPropertyGet(
        // 				context_I.HCtx,
        // 				(byte)extTagIndex_I,
        // 				(byte)tabletIndex_I,
        // 				(byte)controlIndex_I,
        // 				(byte)functionIndex_I,
        // 				(ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_ICON_HEIGHT,
        // 				ref iconHeight))
        // 			{ throw new Exception("Oops - Failed ControlPropertyGet for TABLET_PROPERTY_ICON_HEIGHT"); }

        // 			return SetIcon(context_I, extTagIndex_I, tabletIndex_I, controlIndex_I, functionIndex_I, imageFilePath_I);
        // 		}
        // 	}
        // 	catch (Exception ex)
        // 	{
        // 		Console.WriteLine(ex.ToString());
        // 	}

        // 	// Not supported by tablet.
        // 	return false;
        // }

        /// <summary>
        /// Write out an image to a tablet's OLED (Organic Light Emitting Diode)
        /// if supported by the tablet (eg: Intuos4).
        /// </summary>
        /// <param name="context_I">wintab context</param>
        /// <param name="extTagIndex_I">which extension tag we're setting</param>
        /// <param name="tabletIndex_I">index of the tablet being set</param>
        /// <param name="controlIndex_I">the index of the control being set</param>
        /// <param name="functionIndex_I">the index of the control function being set</param>
        /// <param name="imageFilePath_I">path to PNG image file</param>
        // private static bool SetIcon(
        // 	CWintabContext context_I,
        // 	EWTXExtensionTag extTagIndex_I,
        // 	UInt32 tabletIndex_I,
        // 	UInt32 controlIndex_I,
        // 	UInt32 functionIndex_I,
        // 	String imageFilePath_I)
        // {
        // 	if (!CWintabExtensions.ControlPropertySetImage(
        // 		context_I.HCtx,
        // 		(byte)extTagIndex_I,
        // 		(byte)tabletIndex_I,
        // 		(byte)controlIndex_I,
        // 		(byte)functionIndex_I,
        // 		(ushort)EWTExtensionTabletProperty.TABLET_PROPERTY_OVERRIDE_ICON,
        // 		imageFilePath_I))
        // 	{
        // 		throw new Exception("Oops - FAILED ControlPropertySet for TABLET_PROPERTY_OVERRIDE");
        // 	}
        // 	return true;
        // }
    }
}
