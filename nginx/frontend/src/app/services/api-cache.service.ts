import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ApiCacheService {

  constructor(private api: ApiService) { }

  private data: { [path: string] : Promise<any> } = { };

  getSimpleData(apiPath: string, includeBasePath: boolean = true, httpParams: HttpParams = new HttpParams(), handleApiError: boolean = true): Promise<any> {
    if ( !( apiPath in this.data) ) {
      this.data[apiPath] = this.api.getSimpleData(apiPath, includeBasePath, httpParams, handleApiError).toPromise();
    }
    return this.data[apiPath];
  }
}