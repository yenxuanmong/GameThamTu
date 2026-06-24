# Detective Royale — Backend

Multiplayer murder mystery game server. Players compete in real-time to solve procedurally generated cases.

## Tech Stack

- **Runtime**: Node.js + TypeScript
- **HTTP**: Express 4
- **Real-time**: Socket.IO 4
- **Database**: PostgreSQL via Prisma ORM
- **Cache/Sessions**: Redis (ioredis)
- **AI/LLM**: OpenAI GPT-4o (NPC dialogue + hints)
- **Auth**: JWT (access + refresh tokens)

## Quick Start

```bash
# Install dependencies
npm install

# Copy and configure environment
cp .env.example .env
# Edit .env with your DATABASE_URL, REDIS_URL, OPENAI_API_KEY, etc.

# Generate Prisma client
npm run db:generate

# Run migrations
npm run db:migrate

# Seed initial data (Season 1 + test accounts)
npm run db:seed

# Start development server
npm run dev
```

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `DATABASE_URL` | PostgreSQL connection string | required |
| `REDIS_URL` | Redis connection string | `redis://localhost:6379` |
| `OPENAI_API_KEY` | OpenAI API key for NPC dialogue | required |
| `JWT_SECRET` | JWT signing secret | required |
| `JWT_REFRESH_SECRET` | Refresh token secret | required |
| `FRONTEND_URL` | CORS allowed origin | `http://localhost:5173` |
| `PORT` | Server port | `3000` |
| `NODE_ENV` | Environment | `development` |

## API Endpoints

### Auth — `/api/auth`
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/register` | — | Đăng ký tài khoản |
| POST | `/login` | — | Đăng nhập |
| POST | `/refresh` | — | Làm mới access token |
| POST | `/forgot-password` | — | Gửi email reset mật khẩu |
| GET | `/reset-password/verify` | — | Kiểm tra token reset còn hợp lệ không |
| POST | `/reset-password` | — | Đặt mật khẩu mới |
| GET | `/me` | ✓ | Xem profile |
| PATCH | `/me` | ✓ | Cập nhật profile / preferences |
| POST | `/me/change-password` | ✓ | Đổi mật khẩu (khi đã đăng nhập) |
| POST | `/me/avatar` | ✓ | Upload avatar (multipart/form-data, field: `avatar`, max 2MB) |
| DELETE | `/me/avatar` | ✓ | Xóa avatar |
| POST | `/logout` | ✓ | Đăng xuất |

### Rooms — `/api/rooms`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | — | Danh sách phòng public (filter: `?difficulty=`, `?page=`, `?pageSize=`) |
| GET | `/:roomId` | — | Chi tiết phòng |
| GET | `/:roomId/players` | — | Danh sách người trong phòng |
| POST | `/` | ✓ | Tạo phòng mới qua REST |
| DELETE | `/:roomId` | ✓ | Xóa phòng (host only) |

### Cases — `/api/cases`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | — | List cases (paginated, filterable by difficulty) |
| GET | `/:caseId` | — | Case details |
| GET | `/:caseId/suspects` | — | Suspects (no killer flag) |
| GET | `/:caseId/witnesses` | — | Witnesses (no hidden facts) |
| GET | `/:caseId/evidence` | — | Evidence list |
| GET | `/:caseId/timeline` | — | Public timeline events |
| GET | `/:caseId/stats` | — | Case solve statistics |
| GET | `/:caseId/solution` | ✓ | Solution (only after match ends; requires `?matchId=`) |

### Matches — `/api/matches`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/me` | ✓ | Player's recent matches |
| GET | `/:matchId` | ✓ | Match state |
| GET | `/:matchId/players` | ✓ | Players in match |
| GET | `/:matchId/scores` | ✓ | Scores (only when finished) |
| GET | `/:matchId/progress` | ✓ | My investigation progress |

### Evidence — `/api/matches/:matchId/evidence`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | ✓ | My discovered evidence |
| GET | `/:evidenceId` | ✓ | Evidence detail |
| PATCH | `/:evidenceId/notes` | ✓ | Update notes |
| POST | `/:evidenceId/share` | ✓ | Share with another player |
| GET | `/notebook` | ✓ | Notebook entries |
| DELETE | `/notebook/:entryId` | ✓ | Delete notebook entry |

### Ranking — `/api/ranking`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/leaderboard` | — | Season leaderboard (paginated) |
| GET | `/me` | ✓ | My rank + nearby players |
| GET | `/me/history` | ✓ | Match history |
| GET | `/player/:playerId` | — | Another player's rank |
| GET | `/seasons` | — | All seasons |
| GET | `/seasons/current` | — | Active season info |

### Analytics — `/api/analytics`
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/me` | ✓ | My performance report |
| GET | `/me/accuracy` | ✓ | Accuracy breakdown |
| GET | `/me/trend` | ✓ | Score trend (last N matches) |
| GET | `/matches/:matchId` | ✓ | Match analytics summary |
| GET | `/cases/:caseId` | — | Case accuracy stats |
| GET | `/global` | — | Global game statistics |

## Socket.IO Events

### Client → Server
| Event | Payload | Description |
|---|---|---|
| `room:create` | `{ settings }` | Create a room |
| `room:join` | `{ roomId, password? }` | Join a room |
| `room:leave` | `{ roomId }` | Leave a room |
| `room:ready` | `{ roomId }` | Toggle ready status |
| `queue:join` | `{ difficulty, region? }` | Join matchmaking queue |
| `queue:leave` | — | Leave queue |
| `match:submit_conclusion` | `{ matchId, conclusion }` | Submit case solution |
| `match:request_hint` | `{ matchId }` | Request a hint |
| `investigation:examine_evidence` | `{ matchId, evidenceId }` | Examine a piece of evidence |
| `investigation:interrogate_witness` | `{ matchId, witnessId, message }` | Send message to NPC witness |
| `investigation:add_note` | `{ matchId, note, relatedId? }` | Add notebook entry |

### Server → Client
| Event | Payload | Description |
|---|---|---|
| `room:joined` | `{ room, playerId }` | Joined a room |
| `room:updated` | `{ room }` | Room state changed |
| `room:countdown` | `{ seconds }` | Match starting countdown |
| `match:started` | `{ matchId, caseId }` | Match started |
| `match:phase_changed` | `{ phase, timeRemaining }` | Phase transition |
| `match:timer` | `{ timeRemaining }` | Timer tick |
| `match:player_submitted` | `{ playerId, submittedAt }` | A player submitted |
| `match:ended` | `{ scores, winnerId }` | Match finished |
| `investigation:evidence_found` | `{ evidenceId, playerId }` | Evidence discovered |
| `investigation:hint` | `{ hint, hintsRemaining }` | Hint delivered |
| `npc:response` | `{ witnessId, message, stressLevel }` | NPC replied |
| `error` | `{ code, message }` | Error notification |
| `notification` | `{ type, message }` | Info notification |

### WebRTC Voice (via voiceGateway)
| Event | Description |
|---|---|
| `voice:offer` / `voice:answer` | WebRTC negotiation relay |
| `voice:ice_candidate` | ICE candidate relay |
| `voice:mute` | Mute status broadcast |
| `voice:leave` | Player left voice channel |

## Development Scripts

```bash
npm run dev          # Start dev server with hot-reload
npm run build        # Compile TypeScript
npm run typecheck    # Type check without emit
npm run lint         # ESLint
npm run lint:fix     # ESLint with autofix
npm run test         # Run tests (vitest)
npm run db:studio    # Prisma Studio (DB GUI)
npm run db:reset     # Reset + re-migrate database
```

## Architecture

```
src/
├── app.ts                  # Entry point, Express + Socket.IO setup
├── configs/                # DB, Redis, OpenAI, Socket config
├── controllers/            # HTTP request handlers
├── routes/                 # Express route definitions
├── services/               # Business logic layer
├── middleware/             # Auth, rate limiting, error handling
├── types/                  # TypeScript interfaces
├── ai/
│   ├── npc/                # LLM-based NPC dialogue (OpenAI)
│   ├── hints/              # Contextual hint generation
│   ├── suspicion/          # Player investigation tracking
│   ├── difficulty/         # Adaptive difficulty system
│   ├── director/           # AI narrative director
│   └── witness/            # Witness behaviour systems
├── murder_engine/
│   ├── engine/             # Case generation + scoring
│   ├── generators/         # Suspect, evidence, timeline generators
│   ├── models/             # Case data models
│   └── validators/         # Case validation
├── server/
│   ├── socket/             # Socket.IO event handlers
│   ├── matchmaking/        # Queue + matchmaking logic
│   ├── ranking/            # Leaderboard + season system
│   ├── rooms/              # Room management
│   └── voice/              # WebRTC signalling gateway
├── analytics/              # Player + match + accuracy analytics
└── database/
    ├── prisma/schema.prisma
    └── seed.ts
```
