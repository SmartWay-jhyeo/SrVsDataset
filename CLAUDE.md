# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

이 저장소는 차선 자동 재도색 AI 모델 개발을 위한 데이터셋 관리 시스템인 SrVsDataset 프로젝트를 포함하고 있습니다. 이 시스템은 도로 차선의 비디오 데이터를 수집하고 정리하여 자동 차선 재도색 애플리케이션에 활용하도록 설계되었습니다.

## 아키텍처

저장소는 두 가지 주요 구성 요소로 이루어져 있습니다:

1. **SrVsDataset** - 데이터셋 관리를 위한 메인 WPF 애플리케이션 (.NET 8.0)
   - 위치: `/SrVsDataset/`
   - Windows 데스크톱용 WPF로 구축
   - 진입점: `MainWindow.xaml.cs`

2. **Sample 카메라 SDK** - 여러 언어로 작성된 카메라 통합 예제
   - C# 샘플: `/Sample/C#/` (Basic, Advanced, FirstStep 프로젝트)
   - C++ 샘플: `/Sample/VC++/` (Basic, Advanced, FirstStep 프로젝트)
   - Python 샘플: `/Sample/Python/python_demo.zip`
   - 카메라 작업에 MVSDK 사용

## 주요 명령어

### 메인 애플리케이션 빌드
```bash
# WPF 애플리케이션 빌드
cd SrVsDataset
dotnet build

# 애플리케이션 실행
dotnet run
```

### C# 카메라 샘플 빌드
```bash
# 모든 C# 샘플 빌드
cd Sample/C#
msbuild Demo.sln /p:Configuration=Release

# 또는 개별 프로젝트 빌드
cd Sample/C#/Basic
dotnet build
```

## 핵심 기술 세부사항

- **카메라 통합**: 프로젝트는 카메라 작업을 위해 MVSDK (Machine Vision SDK)를 사용
- **데이터셋 구조**: `데이터셋구성방안.md`에 정의된 계층적 구조를 따름:
  - 도로 종류별 구성 (highway/urban)
  - 날씨 조건 (clear/cloudy)
  - 시간대 (am/midday/pm/night)
  - 녹화 위치 (left/right)
- **파일 명명 규칙**: `YYYYMMDD_HHMMSS(시작)_HHMMSS(종료)_[위치].mp4/json`
- **메타데이터 형식**: GPS 좌표, 타임스탬프, 하드웨어 정보를 포함하는 JSON 파일

## 중요 파일

- `데이터셋구성방안.md`: 데이터셋 구조 및 수집 방법론 문서
- `Sample/C#/MVSDK/MVSDK.cs`: 카메라 SDK용 C# 래퍼
- `Sample/VC++/Include/CameraApi.h`: 카메라 API 정의