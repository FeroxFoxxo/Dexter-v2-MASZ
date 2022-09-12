import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { ChartConfiguration, ChartDataset, ChartType } from 'chart.js';
import { BaseChartDirective, NgChartsModule } from 'ng2-charts';
import { Observable } from 'rxjs';
import 'chartjs-adapter-moment';

@Component({
  selector: 'app-count-chart',
  templateUrl: './count-chart.component.html',
  styleUrls: ['./count-chart.component.css']
})
export class CountChartComponent implements OnInit {

  @ViewChild(BaseChartDirective) chart: BaseChartDirective | undefined;
  @Input() titleKey: string = '';
  @Input() max!: Observable<number>;
  @Input() chartLoading: boolean = true;
  @Input() public chartData : ChartDataset[] = [];
  @Input() public chartLabels: string[] = [];

  public chartOptions: ChartConfiguration['options'] = {
    plugins: {
      title: {
        text: '',
        display: true,
        color: 'rgba(232, 230, 227, 1)'
      },
      legend: {
        display: false
      }
    },
    responsive: false,
    maintainAspectRatio: false,
    borderColor: 'rgb(172, 0, 15)',
    backgroundColor: '#e91e63',
    scales: {
      xAxes: {
        type: 'timeseries',
        time: {
          unit: 'month'
        },
        grid: {
          color: "rgba(0, 0, 0, 0)",
        }
      },
      yAxes: {
        min: 0,
        grid: {
          color: "rgba(0, 0, 0, 0)",
        },
        ticks: {
          callback: function(label: any, index: any, labels: any) {
            // when the floored value is the same as the value we have a whole number
            if (Math.floor(label) === label) {
                return label;
            }
          },
        }
      }
    }
  };

  public chartType: ChartType = 'bar';

  constructor(private translator: TranslateService) { }

  ngOnInit(): void {
    this.chartOptions!.plugins!.title!.text = this.translator.instant(this.titleKey);
    this.translator.onLangChange.subscribe(() => {
      this.chartOptions!.plugins!.title!.text = this.translator.instant(this.titleKey);
      this.chart?.update();
    });

    this.max?.subscribe((data) => {
      this.chartOptions!.scales!.yAxes!.max = data;
    });
  }
}
