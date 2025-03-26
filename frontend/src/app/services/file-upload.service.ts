import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UploadResponse {
  url: string;
  key: string;
}

@Injectable({
  providedIn: 'root'
})
export class FileUploadService {
  private apiUrl = `${environment.apiUrl}/api/upload`;

  constructor(private http: HttpClient) {}

  uploadFile(file: File, folder: string): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folder', folder);

    return this.http.post<UploadResponse>(`${this.apiUrl}/file`, formData);
  }

  deleteFile(fileUrl: string): Observable<any> {
    // Extract the key from the URL
    const key = fileUrl.split('/').pop();
    return this.http.delete(`${this.apiUrl}/file/${key}`);
  }
}
