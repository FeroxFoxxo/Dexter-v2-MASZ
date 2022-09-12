import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'timerFormat'
})
export class TimerFormatPipe implements PipeTransform {

  transform(value: number, digits: number): string {
    let flag = ""
    if (value < 0) {
      flag = "-"
      value *= -1;
    }

    let mins = Math.floor(value / 60);
    let secs = value % 60;

    let minstr = mins.toString().padStart(2, '0');
    digits -= minstr.length + 2;
    if (digits < 0) digits = 0;
    let secstr = secs < 10 ? "0" : "";
    secstr += mins >= 100 ? secs.toFixed(digits) : secs.toFixed(digits);

    return `${flag}${minstr}:${secstr}`
  }

}
