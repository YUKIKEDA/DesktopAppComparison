package com.example.todoappkotlinmultiplatform.ui.components

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.example.todoappkotlinmultiplatform.ui.theme.BrandBlue
import com.example.todoappkotlinmultiplatform.ui.theme.BrandRed

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun Toolbar(
    selectedCount: Int,
    isDarkTheme: Boolean,
    onAddClick: () -> Unit,
    onDeleteClick: () -> Unit,
    onCopyClick: () -> Unit,
    onExportClick: () -> Unit,
    onImportClick: () -> Unit,
    onOpenDataFolderClick: () -> Unit,
    onThemeToggleClick: () -> Unit,
    onOpenInNewWindowClick: (() -> Unit)? = null,
    modifier: Modifier = Modifier
) {
    Surface(
        modifier = modifier.fillMaxWidth(),
        color = MaterialTheme.colorScheme.surface,
        shadowElevation = 2.dp
    ) {
        FlowRow(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Button(
                onClick = onAddClick,
                colors = ButtonDefaults.buttonColors(containerColor = BrandBlue)
            ) {
                Text("+ 新しいアイテム")
            }

            Button(
                onClick = onDeleteClick,
                enabled = selectedCount > 0,
                colors = ButtonDefaults.buttonColors(containerColor = BrandRed)
            ) {
                Text("削除 ($selectedCount)")
            }

            if (onOpenInNewWindowClick != null) {
                OutlinedButton(
                    onClick = onOpenInNewWindowClick,
                    enabled = selectedCount == 1
                ) {
                    Text("別ウィンドウで開く")
                }
            }

            OutlinedButton(
                onClick = onCopyClick,
                enabled = selectedCount > 0
            ) {
                Text("コピー")
            }

            OutlinedButton(onClick = onExportClick) {
                Text("エクスポート")
            }

            OutlinedButton(onClick = onImportClick) {
                Text("インポート")
            }

            OutlinedButton(onClick = onOpenDataFolderClick) {
                Text("データフォルダを開く")
            }

            OutlinedButton(onClick = onThemeToggleClick) {
                Text(if (isDarkTheme) "テーマ: ダーク" else "テーマ: ライト")
            }
        }
    }
}
