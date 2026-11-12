import { BaseApi } from './BaseApi';
import type { ForecastResponseDto } from '../models';

export interface IForecastService {
  getForecast(days: number): Promise<ForecastResponseDto>;
}

export class ForecastService
  extends BaseApi
  implements IForecastService
{
  public constructor() {
    super(true);
  }

  public getForecast = async (
    days: number
  ): Promise<ForecastResponseDto> => {
    const response = await this.get<ForecastResponseDto>(
      `api/forecast?days=${days}`
    );

    return response.data;
  };
}
