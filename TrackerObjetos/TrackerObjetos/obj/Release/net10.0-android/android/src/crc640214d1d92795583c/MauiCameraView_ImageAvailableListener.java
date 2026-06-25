package crc640214d1d92795583c;


public class MauiCameraView_ImageAvailableListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.media.ImageReader.OnImageAvailableListener
{

	public MauiCameraView_ImageAvailableListener ()
	{
		super ();
		if (getClass () == MauiCameraView_ImageAvailableListener.class) {
			mono.android.TypeManager.Activate ("Camera.MAUI.Platforms.Android.MauiCameraView+ImageAvailableListener, Camera.MAUI", "", this, new java.lang.Object[] {  });
		}
	}

	public MauiCameraView_ImageAvailableListener (crc640214d1d92795583c.MauiCameraView p0)
	{
		super ();
		if (getClass () == MauiCameraView_ImageAvailableListener.class) {
			mono.android.TypeManager.Activate ("Camera.MAUI.Platforms.Android.MauiCameraView+ImageAvailableListener, Camera.MAUI", "Camera.MAUI.Platforms.Android.MauiCameraView, Camera.MAUI", this, new java.lang.Object[] { p0 });
		}
	}

	public void onImageAvailable (android.media.ImageReader p0)
	{
		n_onImageAvailable (p0);
	}

	private native void n_onImageAvailable (android.media.ImageReader p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
