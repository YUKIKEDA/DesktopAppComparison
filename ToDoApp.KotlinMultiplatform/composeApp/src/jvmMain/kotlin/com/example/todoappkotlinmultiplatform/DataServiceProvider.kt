package com.example.todoappkotlinmultiplatform

import com.example.todoappkotlinmultiplatform.service.DataService

actual fun getDataService(): com.example.todoappkotlinmultiplatform.model.IDataService = DataService()

