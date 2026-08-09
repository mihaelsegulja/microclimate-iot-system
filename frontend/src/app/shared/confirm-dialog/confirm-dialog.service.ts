import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { map } from 'rxjs';
import { ConfirmDialogComponent, ConfirmDialogData } from './confirm-dialog';

@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly dialog = inject(MatDialog);

  open(options: ConfirmDialogData) {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: options,
      width: '420px'
    });
    return ref.afterClosed().pipe(map((result) => result === true));
  }
}