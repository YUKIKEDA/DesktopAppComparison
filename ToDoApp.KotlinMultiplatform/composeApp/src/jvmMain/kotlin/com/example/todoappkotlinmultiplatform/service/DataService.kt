package com.example.todoappkotlinmultiplatform.service

import com.example.todoappkotlinmultiplatform.model.IDataService
import com.example.todoappkotlinmultiplatform.model.ProjectData
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.awt.Desktop
import java.io.File
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.Paths
import javax.swing.JFileChooser
import javax.swing.filechooser.FileNameExtensionFilter

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

    override suspend fun loadData(): ProjectData = withContext(Dispatchers.IO) {
        try {
            if (!Files.exists(dataDir)) {
                Files.createDirectories(dataDir)
            }
            if (!Files.exists(dataFile)) {
                return@withContext ProjectData()
            }
            val content = Files.readString(dataFile)
            json.decodeFromString<ProjectData>(content)
        } catch (e: Exception) {
            e.printStackTrace()
            ProjectData()
        }
    }

    override suspend fun saveData(data: ProjectData): Unit = withContext(Dispatchers.IO) {
        try {
            if (!Files.exists(dataDir)) {
                Files.createDirectories(dataDir)
            }
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
                val selectedFile = fileChooser.selectedFile
                val content = Files.readString(selectedFile.toPath())
                val data = json.decodeFromString<ProjectData>(content)
                Result.success(data)
            } else {
                Result.failure(Exception("Import cancelled"))
            }
        } catch (e: Exception) {
            e.printStackTrace()
            Result.failure(e)
        }
    }

    override suspend fun openDataFolder(): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            if (!Files.exists(dataDir)) {
                Files.createDirectories(dataDir)
            }
            Desktop.getDesktop().open(dataDir.toFile())
            Result.success(Unit)
        } catch (e: Exception) {
            e.printStackTrace()
            Result.failure(e)
        }
    }
}
