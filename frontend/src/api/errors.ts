const messages: Record<string, string> = {
  'Wallet.NotEnoughPoints': 'Niewystarczająca liczba punktów w portfelu.',
  'Chore.NotFound': 'To zadanie już nie istnieje.',
  'Chore.TitleRequired': 'Tytuł zadania nie może być pusty.',
  'Chore.TitleTooLong': 'Tytuł zadania może mieć maksymalnie 255 znaków.',
  'Chore.DescriptionTooLong': 'Opis może mieć maksymalnie 2000 znaków.',
  'Chore.PointsTooLow': 'Zadanie musi być warte co najmniej 1 punkt.',
  'Chore.CadenceTooShort': 'Częstotliwość musi wynosić co najmniej 1 dzień.',
  'Chore.TagTooLong': 'Tag może mieć maksymalnie 50 znaków.',
  'RewardItem.NotFound': 'Ta nagroda już nie istnieje.',
  'RewardItem.Unavailable': 'Wybrana nagroda nie jest już dostępna.',
  'RewardItem.TitleRequired': 'Wpisz nazwę nagrody.',
  'RewardItem.TitleTooLong': 'Nazwa nagrody może mieć maksymalnie 255 znaków.',
  'RewardItem.DescriptionTooLong': 'Opis może mieć maksymalnie 2000 znaków.',
  'RewardItem.PointCostTooLow': 'Koszt nagrody musi wynosić co najmniej 1 punkt.',
  'RewardItem.QuantityTooLow': 'Liczba sztuk musi wynosić co najmniej 1.',
  'Voucher.NotFound': 'Ten kupon już nie istnieje.',
  'Voucher.AlreadyRedeemed': 'Kupon został już zrealizowany.',
}

export async function apiErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const body: unknown = await response.json()
    const code = typeof body === 'object' && body !== null && 'code' in body ? body.code : undefined
    if (typeof code === 'string' && code in messages) {
      return messages[code]
    }
  } catch {
    return fallback
  }
  return fallback
}
