import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { LinkService, GetLinkByTokenResponse } from '../../services/link.service';
import { ApiResult } from '../../models/api-result';

@Injectable({ providedIn: 'root' })
export class SharedFileResolver implements Resolve<GetLinkByTokenResponse | null> {
  constructor(private linkService: LinkService) {}

  resolve(route: ActivatedRouteSnapshot): Observable<GetLinkByTokenResponse | null> {
    const token = route.paramMap.get('token')!;
    return this.linkService.getLinkByToken(token).pipe(
      map((res: ApiResult<GetLinkByTokenResponse>) =>
        res.success && res.data ? res.data : null
      ),
      catchError(() => of(null))
    );
  }
}
