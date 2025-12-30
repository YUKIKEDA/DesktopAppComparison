package com.example.todoappkotlinmultiplatform.model

data class FilterConfig(
    val columnId: String,
    val type: FilterType,
    val value: FilterValue
)

enum class FilterType {
    TEXT,
    DATE,
    SELECT
}

sealed class FilterValue {
    data class Text(val value: String) : FilterValue()
    data class DateRange(val from: String?, val to: String?) : FilterValue()
    data class Select(val values: List<String>) : FilterValue()
}
