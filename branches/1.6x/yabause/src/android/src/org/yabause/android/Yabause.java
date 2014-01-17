/*  Copyright 2011 Guillaume Duhamel

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

package org.yabause.android;

import java.lang.Runnable;

import android.app.Activity;
import android.os.Bundle;
import android.os.Handler;
import android.os.Message;
import android.util.Log;
import android.graphics.Bitmap;
import android.view.Menu;
import android.view.MenuItem;
import android.view.MenuInflater;
import android.app.Dialog;
import android.app.AlertDialog;
import android.app.AlertDialog.Builder;
import android.content.DialogInterface;
import org.yabause.android.YabauseView;
import android.widget.ImageView;
import android.view.MotionEvent;
import android.view.View;
import android.view.View.OnTouchListener;

class InputHandler extends Handler {
    private YabauseRunnable yr;

    public InputHandler(YabauseRunnable yr) {
        this.yr = yr;
    }

    public void handleMessage(Message msg) {
        if (msg.arg1 == 1) {
            yr.press(msg.arg2);
        } else if (msg.arg1 == 2) {
            yr.release(msg.arg2);
        }
    }
}

class YabauseRunnable implements Runnable
{
    public static native int init(Yabause yabause, Bitmap bitmap);
    public static native void deinit();
    public static native void exec();
    public static native void press(int key);
    public static native void release(int key);
    public static native int initViewport( int width, int hieght);
    public static native int drawScreen();
    public static native int lockGL();
    public static native int unlockGL();
    
    private boolean inited;
    private boolean paused;
    public InputHandler handler;

    public YabauseRunnable(Yabause yabause, Bitmap bitmap)
    {
        handler = new InputHandler(this);
        int ok = init(yabause, bitmap);
        inited = (ok == 0);
    }

    public void pause()
    {
        Log.v("Yabause", "pause... should really pause emulation now...");
        paused = true;
    }

    public void resume()
    {
        Log.v("Yabause", "resuming emulation...");
        paused = false;
        handler.post(this);
    }

    public void destroy()
    {
        Log.v("Yabause", "destroying yabause...");
        inited = false;
        deinit();
    }

    public void run()
    {
        if (inited && (! paused))
        {
            exec();
            
            handler.post(this);
        }
    }

    public boolean paused()
    {
        return paused;
    }
}

class YabauseHandler extends Handler {
    private Yabause yabause;

    public YabauseHandler(Yabause yabause) {
        this.yabause = yabause;
    }

    public void handleMessage(Message msg) {
        yabause.showDialog(msg.what, msg.getData());
    }
}

public class Yabause extends Activity implements OnTouchListener
{
    private static final String TAG = "Yabause";
    private YabauseRunnable yabauseThread;
    private YabauseHandler handler;

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);

        setContentView(R.layout.main);

        YabauseView view = (YabauseView) findViewById(R.id.yabause_view);
        handler = new YabauseHandler(this);
        yabauseThread = new YabauseRunnable(this,null);
        view.setYabauseRunnable(yabauseThread);

        ImageView pad = (ImageView) findViewById(R.id.yabause_pad);
        pad.setOnTouchListener(this);
    }

    @Override
    public void onPause()
    {
        super.onPause();
        Log.v(TAG, "pause... should pause emulation...");
        yabauseThread.pause();
    }

    @Override
    public void onResume()
    {
        super.onResume();
        Log.v(TAG, "resume... should resume emulation...");
        yabauseThread.resume();
    }

    @Override
    public void onDestroy()
    {
        super.onDestroy();
        Log.v(TAG, "this is the end...");
        yabauseThread.destroy();
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        MenuInflater inflater = getMenuInflater();
        inflater.inflate(R.menu.emulation, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        switch (item.getItemId()) {
        case R.id.pause:
            yabauseThread.pause();
            return true;
        case R.id.quit:
            this.finish();
            return true;
        case R.id.resume:
            yabauseThread.resume();
            return true;
        default:
            return super.onOptionsItemSelected(item);
        }
    }

    @Override
    public boolean onPrepareOptionsMenu(Menu menu) {
        if (yabauseThread.paused()) {
            menu.setGroupVisible(R.id.paused, true);
            menu.setGroupVisible(R.id.running, false);
        } else {
            menu.setGroupVisible(R.id.paused, false);
            menu.setGroupVisible(R.id.running, true);
        }
        return true;
    }

    @Override
    public Dialog onCreateDialog(int id, Bundle args) {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setMessage(args.getString("message"))
            .setCancelable(false)
            .setNegativeButton("Exit", new DialogInterface.OnClickListener() {
                public void onClick(DialogInterface dialog, int id) {
                    Yabause.this.finish();
                }
            })
            .setPositiveButton("Ignore", new DialogInterface.OnClickListener() {
                public void onClick(DialogInterface dialog, int id) {
                    dialog.cancel();
                }
            });
        AlertDialog alert = builder.create();
        return alert;
    }

    public boolean onTouch(View v, MotionEvent event) {
        int action = event.getActionMasked();
        float x = event.getX();
        float y = event.getY();
        int keyx = (int) ((x - 10) / 30);
        int keyy = (int) ((y - 10) / 30);
        int key = (keyx << 2) | keyy;
        int keya = 0;
        if (action == event.ACTION_DOWN) {
            keya = 1;
        } else if (action == event.ACTION_UP) {
            keya = 2;
        }

        Message message = handler.obtainMessage();
        message.arg1 = keya;
        message.arg2 = key;
        yabauseThread.handler.sendMessage(message);

        return true;
    }

    private void errorMsg(String msg) {
        Message message = handler.obtainMessage();
        Bundle bundle = new Bundle();
        bundle.putString("message", msg);
        message.setData(bundle);
        handler.sendMessage(message);
    }

    static {
        System.loadLibrary("yabause");
    }
}
