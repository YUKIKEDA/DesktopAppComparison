package com.example.todoappkotlinmultiplatform.util

import java.time.Instant
import java.time.LocalDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter

actual fun formatDateISO(dateString: String): String {
    return try {
        val instant = Instant.parse(dateString)
        val localDateTime = LocalDateTime.ofInstant(instant, ZoneId.systemDefault())
        localDateTime.format(DateTimeFormatter.ofPattern("yyyy-MM-dd"))
    } catch (e: Exception) {
        dateString
    }
}

actual fun formatDateTimeISO(dateString: String): String {
    return try {
        val instant = Instant.parse(dateString)
        val localDateTime = LocalDateTime.ofInstant(instant, ZoneId.systemDefault())
        localDateTime.format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm"))
    } catch (e: Exception) {
        dateString
    }
}
