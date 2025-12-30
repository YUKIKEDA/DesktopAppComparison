package com.example.todoappkotlinmultiplatform.ui.components

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun Toolbar(
    selectedCount: Int,
    onAddClick: () -> Unit,
    onDeleteClick: () -> Unit,
    onExportClick: () -> Unit,
    onImportClick: () -> Unit,
    onOpenDataFolderClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Surface(
        modifier = modifier.fillMaxWidth(),
        color = MaterialTheme.colorScheme.surface,
        shadowElevation = 2.dp
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalAlignment = androidx.compose.ui.Alignment.CenterVertically
        ) {
            Button(onClick = onAddClick) {
                Text("+ 新しいアイテム")
            }
            
            Button(
                onClick = onDeleteClick,
                enabled = selectedCount > 0,
                colors = ButtonDefaults.buttonColors(
                    containerColor = MaterialTheme.colorScheme.error
                )
            ) {
                Text("削除 ($selectedCount)")
            }
            
            Spacer(modifier = Modifier.weight(1f))
            
            OutlinedButton(onClick = onExportClick) {
                Text("エクスポート")
            }
            
            OutlinedButton(onClick = onImportClick) {
                Text("インポート")
            }
            
            OutlinedButton(onClick = onOpenDataFolderClick) {
                Text("データフォルダを開く")
            }
        }
    }
}
