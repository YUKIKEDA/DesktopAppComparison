package com.example.todoappkotlinmultiplatform.service

import com.example.todoappkotlinmultiplatform.model.IDataService
import com.example.todoappkotlinmultiplatform.model.ProjectData
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.awt.Desktop
import java.awt.GraphicsEnvironment
import java.awt.Rectangle
import java.io.File
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.Paths
import javax.swing.JFileChooser
import javax.swing.filechooser.FileNameExtensionFilter

@Serializable
data class WindowGeometry(
    val x: Int = 100,
    val y: Int = 100,
    val width: Int = 1400,
    val height: Int = 900
)

class DataService : IDataService {
    private val json = Json {
        prettyPrint = true
        ignoreUnknownKeys = true
    }

    private val dataDir: Path = Paths.get(
        System.getProperty("user.home"),
        ".todoapp.kotlinmultiplatform",
        "data"
    )
    private val dataFile: Path = dataDir.resolve("project.json")
    private val windowFile: Path = dataDir.resolve("window.json")

    val dataDirectory: Path get() = dataDir

    override suspend fun loadData(): ProjectData = withContext(Dispatchers.IO) {
        try {
            ensureDataDir()
            if (!Files.exists(dataFile)) {
                return@withContext ProjectData()
            }
            parseProjectData(Files.readString(dataFile))
        } catch (e: Exception) {
            e.printStackTrace()
            ProjectData()
        }
    }

    override suspend fun saveData(data: ProjectData): Unit = withContext(Dispatchers.IO) {
        try {
            ensureDataDir()
            val content = json.encodeToString(data)
            Files.writeString(dataFile, content)
        } catch (e: Exception) {
            e.printStackTrace()
            throw e
        }
    }

    override suspend fun exportData(data: ProjectData): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            val fileChooser = JFileChooser().apply {
                dialogTitle = "データをエクスポート"
                fileFilter = FileNameExtensionFilter("JSON Files", "json")
                selectedFile = File("project.json")
            }

            val result = fileChooser.showSaveDialog(null)
            if (result == JFileChooser.APPROVE_OPTION) {
                val selectedFile = fileChooser.selectedFile
                val content = json.encodeToString(data)
                Files.writeString(selectedFile.toPath(), content)
                Result.success(Unit)
            } else {
                Result.failure(Exception("Export cancelled"))
            }
        } catch (e: Exception) {
            e.printStackTrace()
            Result.failure(e)
        }
    }

    override suspend fun importData(): Result<ProjectData> = withContext(Dispatchers.IO) {
        try {
            val fileChooser = JFileChooser().apply {
                dialogTitle = "データをインポート"
                fileFilter = FileNameExtensionFilter("JSON Files", "json")
            }

            val result = fileChooser.showOpenDialog(null)
            if (result == JFileChooser.APPROVE_OPTION) {
                importFromPathInternal(fileChooser.selectedFile.absolutePath)
            } else {
                Result.failure(Exception("Import cancelled"))
            }
        } catch (e: Exception) {
            e.printStackTrace()
            Result.failure(e)
        }
    }

    override suspend fun importFromPath(path: String): Result<ProjectData> = withContext(Dispatchers.IO) {
        importFromPathInternal(path)
    }

    private fun importFromPathInternal(path: String): Result<ProjectData> {
        return try {
            val content = Files.readString(Paths.get(path))
            Result.success(parseProjectData(content))
        } catch (e: Exception) {
            e.printStackTrace()
            Result.failure(e)
        }
    }

    private fun parseProjectData(content: String): ProjectData {
        return json.decodeFromString<ProjectData>(content)
    }

    override suspend fun openDataFolder(): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            ensureDataDir()
            Desktop.getDesktop().open(dataDir.toFile())
            Result.success(Unit)
        } catch (e: Exception) {
            e.printStackTrace()
            Result.failure(e)
        }
    }

    fun loadWindowGeometry(): WindowGeometry {
        return try {
            ensureDataDir()
            if (!Files.exists(windowFile)) {
                return WindowGeometry()
            }
            val geometry = json.decodeFromString<WindowGeometry>(Files.readString(windowFile))
            if (geometry.width < 100 || geometry.height < 100 || !isOnAnyScreen(geometry)) {
                WindowGeometry()
            } else {
                geometry
            }
        } catch (e: Exception) {
            e.printStackTrace()
            WindowGeometry()
        }
    }

    fun saveWindowGeometry(geometry: WindowGeometry) {
        try {
            ensureDataDir()
            Files.writeString(windowFile, json.encodeToString(geometry))
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    private fun ensureDataDir() {
        if (!Files.exists(dataDir)) {
            Files.createDirectories(dataDir)
        }
    }

    private fun isOnAnyScreen(geometry: WindowGeometry): Boolean {
        return try {
            val screens = GraphicsEnvironment.getLocalGraphicsEnvironment().screenDevices
            if (screens.isEmpty()) return true
            val windowRect = Rectangle(geometry.x, geometry.y, geometry.width, geometry.height)
            screens.any { device ->
                device.defaultConfiguration.bounds.intersects(windowRect)
            }
        } catch (_: Exception) {
            true
        }
    }
}
