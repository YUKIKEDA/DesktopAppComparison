package com.example.todoappkotlinmultiplatform.util

import java.time.Instant

actual fun currentTimeISOString(): String = Instant.now().toString()
