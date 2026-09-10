import requests
import json
import os

def fetch_evolution_chains():
    print("PokeAPI에서 전체 진화 체인 정보를 먼저 수집합니다...")
    chain_data_map = {} 
    
    # 🌟 수정됨: 중간에 비어있는 결번이 있으므로 넉넉하게 1번부터 999번까지 탐색합니다.
    for chain_id in range(1, 1000):
        url = f"https://pokeapi.co/api/v2/evolution-chain/{chain_id}"
        res = requests.get(url)
        
        if res.status_code != 200:
            # 🌟 핵심: break 대신 continue를 써서 빈 데이터가 있어도 멈추지 않고 넘어갑니다!
            continue
            
        chain = res.json().get("chain", {})
        
        def parse_chain(node):
            species_info = node.get("species", {})
            species_url = species_info.get("url", "")
            if not species_url:
                return
            current_id = int(species_url.rstrip("/").split("/")[-1])
            
            evolves_to = node.get("evolves_to", [])
            if evolves_to:
                next_node = evolves_to[0]
                next_species_info = next_node.get("species", {})
                next_url = next_species_info.get("url", "")
                if next_url:
                    next_id = int(next_url.rstrip("/").split("/")[-1])
                    
                    evo_details = next_node.get("evolution_details", [])
                    min_level = 0
                    if evo_details and evo_details[0].get("min_level"):
                        min_level = evo_details[0].get("min_level")
                    
                    chain_data_map[current_id] = {
                        "EvolveTo": next_id,
                        "EvolveLevel": min_level if min_level else 36 
                    }
            
            for sub_node in evolves_to:
                parse_chain(sub_node)
                
        parse_chain(chain)
        
        # 진행 상황을 50개마다 출력해 줍니다.
        if chain_id % 50 == 0:
            print(f"진화 체인 탐색 중... ({chain_id}/1000)")
        
    print(f"총 {len(chain_data_map)}개의 진화 관계를 성공적으로 파싱했습니다.")
    return chain_data_map

def fetch_korean_pokemon_data():
    output_dir = "Data"
    output_path = os.path.join(output_dir, "pokemon_data.json")
    
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)

    evolution_map = fetch_evolution_chains()

    pokemon_list = []
    print("\n1번부터 1025번까지의 포켓몬 기본 스펙과 한글 이름을 수집합니다...")

    for poke_id in range(1, 1026):
        pokemon_url = f"https://pokeapi.co/api/v2/pokemon/{poke_id}"
        species_url = f"https://pokeapi.co/api/v2/pokemon-species/{poke_id}"
        
        try:
            res_poke = requests.get(pokemon_url)
            res_species = requests.get(species_url)
            
            if res_poke.status_code == 200 and res_species.status_code == 200:
                data_poke = res_poke.json()
                data_species = res_species.json()
                
                korean_name = f"MON #{poke_id:03d}"
                for entry in data_species.get("names", []):
                    if entry["language"]["name"] == "ko":
                        korean_name = entry["name"]
                        break
                
                types = data_poke.get("types", [])
                type1 = types[0]["type"]["name"].capitalize() if len(types) > 0 else "Normal"
                type2 = types[1]["type"]["name"].capitalize() if len(types) > 1 else "None"
                
                stats = {stat["stat"]["name"]: stat["base_stat"] for stat in data_poke.get("stats", [])}
                
                evo_info = evolution_map.get(poke_id, {"EvolveTo": 0, "EvolveLevel": 0})
                
                poke_info = {
                    "Id": poke_id,
                    "DisplayName": korean_name,
                    "Type1": type1,
                    "Type2": type2,
                    "BaseHp": stats.get("hp", 50),
                    "BaseAtk": stats.get("attack", 50),
                    "BaseDef": stats.get("defense", 50),
                    "BaseSpeed": stats.get("speed", 50),
                    "BaseSpA": stats.get("special-attack", 50),
                    "BaseSpD": stats.get("special-defense", 50),
                    "EvolveTo": evo_info["EvolveTo"],
                    "EvolveLevel": evo_info["EvolveLevel"]
                }
                pokemon_list.append(poke_info)
                
                if poke_id % 50 == 0:
                    print(f"진행 상황: {poke_id}/1025 완료")
            else:
                print(f"[경고] ID {poke_id} 데이터를 가져오지 못했습니다.")
        except Exception as e:
            print(f"[에러] ID {poke_id} 처리 중 예외 발생: {e}")

    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(pokemon_list, f, indent=4, ensure_ascii=False)
    
    print(f"\n[성공] 진화 정보가 완벽하게 포함된 총 {len(pokemon_list)}마리의 데이터가 '{output_path}'에 저장되었습니다!")

if __name__ == "__main__":
    fetch_korean_pokemon_data()