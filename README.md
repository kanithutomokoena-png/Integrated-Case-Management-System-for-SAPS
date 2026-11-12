# Integrated Case Management System (ICMS) for SAPS

An ASP.NET Core MVC case management system for the South African Police Service (SAPS), with an embedded machine learning service that suggests a **crime category** and a **priority level** from the free-text incident narrative. Officers always stay in control: every prediction can be overridden, and overrides are audited.

Developed at the Central University of Technology, Free State, as research into automated case categorisation and prioritisation (student project, beta/prototype).

> **Status:** research prototype. It has not been validated on real SAPS dockets and must not be used for operational policing decisions.

## Research objectives

1. To analyse current technological trends and limitations in machine learning applications for crime analysis, specifically focusing on case classification and prioritisation in law enforcement systems.
2. To investigate current case management practices within the South African Police Service (SAPS) and identify challenges related to case categorisation and prioritisation.
3. To design and implement a machine learning-based model within an Integrated Case Management System (ICMS) that automates case categorisation and priority assignment for SAPS case data.

## How it works

```text
Browser ──► ASP.NET Core MVC (localhost:5140) ──► MySQL (schema: icms)
                    │
                    └──► Flask API (localhost:5001, POST /predict)
                              └── multi-task BiLSTM (TensorFlow/Keras)
                                  returns category, priority, confidence, justification
```

While an officer types a narrative, the form requests a live prediction. On saving, the server calls the model again so the stored result is authoritative. If the model service is down, the case is saved as *Unclassified* and an Admin can classify it later in bulk.

## Features

**Machine learning**
- Category (27 classes) and priority (Low, Medium, High, Critical) predicted from the narrative
- Confidence score and a plain-language justification shown before submission
- Officer override of the prediction; on edit a written reason is required, and who/when/why is recorded
- Admin batch classification of older, unclassified cases

**Case management**
- Login with role-based access: **Admin**, **Detective**, **Investigator**
- Case registration with complainant details, statement, evidence upload and witnesses
- Status workflow (Investigation, Under Review, Closed); only an Admin can close a case
- Edit-request and closure-request workflows: officers request, an Admin approves or rejects
- Private case notes
- Witness Directory with search and repeat-witness filter
- Searchable, filterable case list ("Digital Dockets") with progress indicator
- Crime Analytics dashboard (volume trend, category and priority breakdowns, open and critical cases)
- Password-protected archive of closed cases

## Technologies

| Layer | Technology |
|---|---|
| Web app | ASP.NET Core MVC, C#, Entity Framework Core, ASP.NET Core Identity |
| Front end | Razor views, HTML5, CSS3, JavaScript, Chart.js |
| Database | MySQL |
| ML service | Python 3, Flask, TensorFlow/Keras, scikit-learn, NumPy |
| Tools | Visual Studio / VS Code, Git, MySQL Workbench, .NET SDK |

## The model

A single **multi-task BiLSTM** reads each narrative once and predicts both outputs:

`Embedding(128)` → `BiLSTM(64)` → `GlobalMaxPool` → `Dense(128)` → two heads (`Dense(64)` → softmax): 27 categories and 4 priorities.

- **Data:** 10,132 labelled narratives. 20 are real, publicly released SAPS statements; the rest are synthetic and written in SAPS press-release style (5,705 localised to Bloemfontein suburbs). Real case dockets are protected under POPIA and were not used.
- **Labels:** category labels come from the data generator. **Priority labels are rule-based** (category tier, escalated for weapons, serious injury, high value, minor victims, officials) with small noise. They are not SAPS ground truth.
- **Preprocessing:** lower-casing, punctuation removal, tokenisation (vocabulary limit 8,000), padding to 70 tokens. Stop-word removal and stemming are deliberately not applied.
- **Confidence:** the largest softmax probability of each head; the app shows the average of the two.

### Results (held-out test set, 2,027 narratives)

| Model | Category accuracy | Priority accuracy | Priority macro-F1 |
|---|---|---|---|
| Naive Bayes (TF-IDF) | 98.96% | 79.53% | 0.813 |
| Decision Tree (TF-IDF) | 98.91% | 81.80% | 0.831 |
| Logistic Regression (TF-IDF) | 99.06% | 84.02% | 0.858 |
| Linear SVM (TF-IDF) | 99.11% | 84.56% | 0.862 |
| Random Forest (TF-IDF) | 99.31% | 85.94% | 0.871 |
| **Multi-task BiLSTM (deployed)** | 99.11% | **89.94%** | **0.908** |

### Read these results with care

- The text is mostly synthetic and templated (fewer than 950 distinct words occur), so near-99% category accuracy shows that the pipeline works, **not** that it will perform this well on real dockets.
- The BiLSTM is not better than classical models on category. Its advantage is on priority, and no ablation was run to explain why.
- Only 5 of the 20 real statements fall in the test set, so there is no meaningful accuracy figure for real text.
- On the synthetic test set the confidence is well calibrated (expected calibration error 0.5% category, 1.6% priority), but 140 of 204 priority errors still had 80% or more confidence, and calibration on real or informal text has not been measured.
- Informal, officer-typed wording is weaker than formal wording.

Full methodology, training curves, confusion matrix and calibration charts are in [`ModelDevelopment/ICMS_Model_Development.ipynb`](ModelDevelopment/ICMS_Model_Development.ipynb). The training code is in `ModelDevelopment/train_model_final.py`.

## Project structure

Adjust to match your repository.

```text
Integrated-Case-Management-System-for-SAPS
├── SAPS_ICMS/                  ASP.NET Core MVC application
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   ├── Services/               includes the client for the ML service
│   ├── Data/                   DbContext and migrations
│   ├── wwwroot/
│   ├── Program.cs
│   └── appsettings.json
├── FlaskAPI/                   model service
│   ├── app.py                  POST /predict
│   ├── requirements.txt
│   └── ml_model/               multitask_case_model.keras, tokenizer.pkl,
│                               label_encoders.pkl, model_config.json
├── ModelDevelopment/           notebook and training script
└── README.md
```

## Getting started

### Prerequisites

- .NET SDK, MySQL Server, Python 3.10 or later, Git

### 1. Clone

```bash
git clone https://github.com/kanithutomokoena-png/Integrated-Case-Management-System-for-SAPS.git
cd Integrated-Case-Management-System-for-SAPS
```

### 2. Database

Create an empty MySQL schema named `icms`, then set the connection string in `SAPS_ICMS/appsettings.json`. **Do not commit your real password**; keep it in user secrets or an untracked local settings file:

```bash
cd SAPS_ICMS
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;database=icms;user=YOUR_USER;password=YOUR_PASSWORD"
dotnet restore
dotnet ef database update
```

The connection-string key must match the name your `Program.cs` reads.

### 3. Start the model service first

```bash
cd FlaskAPI
python -m venv venv
venv\Scripts\activate          # macOS/Linux: source venv/bin/activate
pip install -r requirements.txt
python app.py
```

Wait for "Model loaded successfully". The service listens on `http://localhost:5001`. Check it at `http://localhost:5001/`.

### 4. Start the web app

```bash
cd SAPS_ICMS
dotnet run
```

Open `http://localhost:5140`. If the model service is not running, cases still save but as *Unclassified*.

### Users and roles

Create your own Admin, Detective and Investigator accounts through the registration page or your own seed step. No default credentials are shipped with the project.

## Known limitations

- Trained mostly on synthetic data; priority labels are rule-based, not SAPS ground truth.
- Not validated on real SAPS data; informal text is handled less well.
- The Flask service has **no authentication or HTTPS** and listens on all interfaces (`0.0.0.0`). Run it only on a trusted local machine, or bind it to `127.0.0.1`.
- No formal usability study with police users yet.

## Ethics and data protection

No real personal or confidential case data is used. The real-statement rows come from publicly released SAPS media statements. Any future use of real dockets requires ethical approval and POPIA-compliant anonymisation. Model output is advisory; the officer's decision is final.

## Future work

- Validate on anonymised real data under POPIA and ethical approval
- Calibrate confidence on real and informal text (for example temperature scaling)
- Test a pretrained language model (such as DistilBERT) and learn from officer overrides
- Secure the web-app-to-model link with authentication and HTTPS
- Usability study with police-style scenarios
- Email notifications, interactive crime heat maps, mobile app, cloud deployment

## Author

**Kananelo Thuto Mokoena**, Central University of Technology, Free State

## License

Developed for educational and research purposes. "all rights reserved".
