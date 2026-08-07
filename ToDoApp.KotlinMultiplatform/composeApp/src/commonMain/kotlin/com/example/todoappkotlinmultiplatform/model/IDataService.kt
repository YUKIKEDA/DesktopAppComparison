package com.example.todoappkotlinmultiplatform.model

interface IDataService {
    suspend fun loadData(): ProjectData
    suspend fun saveData(data: ProjectData)
    suspend fun exportData(data: ProjectData): Result<Unit>
    suspend fun importData(): Result<ProjectData>
    suspend fun importFromPath(path: String): Result<ProjectData>
    suspend fun openDataFolder(): Result<Unit>
}
