import { Injectable } from '@angular/core';
import { combineLatest, ReplaySubject } from 'rxjs';
import { AppVersion } from '../models/AppVersion';
import { ImageVersion } from '../models/ImageVersion';
import { compare } from 'compare-versions';

@Injectable({
  providedIn: 'root'
})
export class VersionManagerService {

  localVersion: ReplaySubject<AppVersion> = new ReplaySubject<AppVersion>(1);
  availableVersions: ReplaySubject<ImageVersion[]> = new ReplaySubject<ImageVersion[]>(1);
  newVersionFound: ReplaySubject<ImageVersion> = new ReplaySubject<ImageVersion>(1);

  constructor() {
    combineLatest([this.localVersion, this.availableVersions]).subscribe(([newLocalVersion, newAvailableVersions]) => {
      if (newLocalVersion && newAvailableVersions) {
        let newestVersion = newAvailableVersions?.find(x => x !== undefined);
        let localVersionTag = newLocalVersion?.version;
        if (newestVersion != undefined && localVersionTag != undefined) {
          if (compare(newestVersion.tag.replace('a', '-alpha'), localVersionTag, '>')) {
            this.newVersionFound.next(newestVersion);
          }
        }
      }
    });
  }

  localVersionChanged(data: AppVersion) {
    this.localVersion.next(data);
  }

  availableVersionsChanged(data: ImageVersion[]) {
    this.availableVersions.next(data);
  }
}
