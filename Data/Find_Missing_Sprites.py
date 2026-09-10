import requests

def find_missing_sprites():
    # 대상 GitHub API 주소
    url = "https://api.github.com/repos/PMDCollab/SpriteCollab/contents/sprite"
    
    # GitHub API는 User-Agent 헤더를 필수로 요구합니다.
    headers = {'User-Agent': 'Python-MissingNumberFinder'}

    print("GitHub에서 폴더 목록을 가져오는 중입니다. 잠시만 기다려주세요...")

    try:
        # API 호출 및 JSON 응답 받기
        response = requests.get(url, headers=headers)
        response.raise_for_status() # 통신 에러 발생 시 예외 처리
        
        data = response.json()
        
        # 존재하는 번호를 담을 집합(set) 생성 (검색 속도 최적화)
        existing_ids = set()
        
        for item in data:
            # 항목이 폴더(dir)인지, 그리고 이름이 숫자로만 이루어져 있는지 확인
            if item.get('type') == 'dir' and item.get('name').isdigit():
                existing_ids.add(int(item['name']))
        
        # 0부터 1025까지 돌면서 빠진 번호 찾기 (리스트 컴프리헨션 사용)
        missing_ids = [i for i in range(1026) if i not in existing_ids]
        
        # 결과 출력
        print(f"\n완료! 0부터 1025까지 총 {len(missing_ids)}개의 번호가 빠져 있습니다:")
        print(", ".join(map(str, missing_ids)))
        
    except requests.exceptions.RequestException as e:
        print(f"데이터를 가져오는 중 오류가 발생했습니다: {e}")

if __name__ == "__main__":
    find_missing_sprites()