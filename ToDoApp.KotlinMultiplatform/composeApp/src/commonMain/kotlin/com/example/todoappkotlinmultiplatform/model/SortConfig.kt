package com.example.todoappkotlinmultiplatform.model

data class SortConfig(
    val columnId: String,
    val direction: SortDirection
)

enum class SortDirection {
    ASC,
    DESC
}
