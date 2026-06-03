using System.Collections.Generic;

namespace MiyakoCarryService.Client.Utils
{
    public static class LocalLocales
    {
        public static Dictionary<string, Dictionary<string, string>> LoadingLocales = new()
        {
            {
                "ch", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "当前正在尝试加载本地化文本，如果您发现长期处于当前状态，请确保您已正确安装服务端 Mod MiyakoCarryServiceServer，否则无法获取到本地化文本"
                    }
                }
            },
            {
                "cz", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Probíhá načítání lokalizačních textů. Pokud tento stav přetrvává, ujistěte se, že je správně nainstalován serverový mod MiyakoCarryServiceServer – jinak nelze lokalizaci získat"
                    }
                }
            },
            {
                "en", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Attempting to load localization texts. If this persists, ensure the server mod MiyakoCarryServiceServer is installed; otherwise localization cannot be retrieved"
                    }
                }
            },
            {
                "es-mx", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Intentando cargar textos de localización. Si esto persiste, asegúrate de que el mod de servidor MiyakoCarryServiceServer esté instalado; de lo contrario no se podrá obtener la localización"
                    }
                }
            },
            {
                "es", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Intentando cargar textos de localización. Si esto persiste, asegúrate de que el mod de servidor MiyakoCarryServiceServer esté instalado; de lo contrario no se podrá obtener la localización"
                    }
                }
            },
            {
                "fr", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Tentative de chargement des textes de localisation. Si cela persiste, assurez-vous que le mod serveur MiyakoCarryServiceServer est installé, sinon la localisation ne pourra pas être récupérée"
                    }
                }
            },
            {
                "ge", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Lokalisierungstexte werden geladen. Falls dies länger dauert, stellen Sie sicher, dass das Server-Mod MiyakoCarryServiceServer installiert ist – andernfalls kann die Lokalisierung nicht abgerufen werden"
                    }
                }
            },
            {
                "hu", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Lokalizációs szövegek betöltése folyamatban. Ha ez hosszabb ideig tart, győződjön meg róla, hogy a MiyakoCarryServiceServer szerver mod telepítve van – ellenkező esetben a lokalizáció nem érhető el"
                    }
                }
            },
            {
                "it", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Tentativo di caricamento dei testi di localizzazione. Se la situazione persiste, assicurati che la mod lato server MiyakoCarryServiceServer sia installata, altrimenti non sarà possibile ottenere la localizzazione"
                    }
                }
            },
            {
                "jp", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "ローカライズテキストを読み込もうとしています。この状態が続く場合は、サーバーMod「MiyakoCarryServiceServer」が正しくインストールされていることを確認してください。インストールされていない場合、ローカライズテキストを取得できません"
                    }
                }
            },
            {
                "kr", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "로컬라이제이션 텍스트를 로드하는 중입니다. 이 상태가 오래 지속되면 서버 모드 MiyakoCarryServiceServer가 올바르게 설치되었는지 확인하세요. 그렇지 않으면 로컬라이제이션을 가져올 수 없습니다"
                    }
                }
            },
            {
                "pl", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Próba ładowania tekstów lokalizacji. Jeśli ten stan się utrzymuje, upewnij się, że zainstalowany jest mod serwerowy MiyakoCarryServiceServer – w przeciwnym razie lokalizacja nie będzie mogła zostać pobrana"
                    }
                }
            },
            {
                "po", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Tentativa de carregar textos de localização. Se isto persistir, certifique-se de que o mod de servidor MiyakoCarryServiceServer está instalado; caso contrário, a localização não poderá ser obtida"
                    }
                }
            },
            {
                "ro", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Se încearcă încărcarea textelor de localizare. Dacă această stare persistă, asigurați-vă că mod-ul de server MiyakoCarryServiceServer este instalat corect – altfel localizarea nu poate fi obținută"
                    }
                }
            },
            {
                "ru", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Попытка загрузки текстов локализации. Если это состояние сохраняется, убедитесь, что установлен серверный мод MiyakoCarryServiceServer, иначе локализация не может быть получена"
                    }
                }
            },
            {
                "sk", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Prebieha načítanie lokalizačných textov. Ak tento stav pretrváva, uistite sa, že je nainštalovaný serverový mod MiyakoCarryServiceServer – inak nie je možné lokalizáciu získať"
                    }
                }
            },
            {
                "tu", new()
                {
                    {
                        "Mcs/LoadingLocales",
                        "Yerelleştirme metinleri yüklenmeye çalışılıyor. Bu durum uzun sürerse, sunucu modu MiyakoCarryServiceServer'ın doğru şekilde kurulduğundan emin olun; aksi halde yerelleştirme alınamaz"
                    }
                }
            }
        };
    }
}
