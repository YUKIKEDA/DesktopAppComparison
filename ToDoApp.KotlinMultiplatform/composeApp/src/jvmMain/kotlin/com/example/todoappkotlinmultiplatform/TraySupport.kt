package com.example.todoappkotlinmultiplatform

import java.awt.Color
import java.awt.Image
import java.awt.MenuItem
import java.awt.PopupMenu
import java.awt.SystemTray
import java.awt.TrayIcon
import java.awt.image.BufferedImage
import javax.swing.SwingUtilities

/**
 * AWT system tray + balloon notifications for Compose Desktop.
 */
object TraySupport {
    private var trayIcon: TrayIcon? = null

    fun isSupported(): Boolean = SystemTray.isSupported()

    fun install(
        onShow: () -> Unit,
        onQuit: () -> Unit
    ) {
        if (!SystemTray.isSupported() || trayIcon != null) return
        try {
            val icon = TrayIcon(createTrayImage(), "Todo App", createMenu(onShow, onQuit))
            icon.isImageAutoSize = true
            icon.addActionListener {
                SwingUtilities.invokeLater(onShow)
            }
            SystemTray.getSystemTray().add(icon)
            trayIcon = icon
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    fun notify(title: String, message: String) {
        val icon = trayIcon
        if (icon != null) {
            SwingUtilities.invokeLater {
                icon.displayMessage(title, message, TrayIcon.MessageType.INFO)
            }
        }
    }

    fun uninstall() {
        val icon = trayIcon ?: return
        try {
            SystemTray.getSystemTray().remove(icon)
        } catch (_: Exception) {
        }
        trayIcon = null
    }

    private fun createMenu(onShow: () -> Unit, onQuit: () -> Unit): PopupMenu {
        val menu = PopupMenu()
        val showItem = MenuItem("表示")
        showItem.addActionListener { SwingUtilities.invokeLater(onShow) }
        menu.add(showItem)
        val quitItem = MenuItem("終了")
        quitItem.addActionListener { SwingUtilities.invokeLater(onQuit) }
        menu.add(quitItem)
        return menu
    }

    private fun createTrayImage(): Image {
        val size = 16
        val image = BufferedImage(size, size, BufferedImage.TYPE_INT_ARGB)
        val g = image.createGraphics()
        g.color = Color(37, 99, 235)
        g.fillOval(1, 1, size - 3, size - 3)
        g.color = Color.WHITE
        g.fillRect(5, 4, 2, 8)
        g.fillRect(9, 4, 2, 8)
        g.dispose()
        return image
    }
}
