//////////////////////////////// 
// 
//   Copyright 2018 Battelle Energy Alliance, LLC  
// 
// 
//////////////////////////////// 
using System;
using System.Collections.Generic;

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CSET_Main.Views.Questions.QuestionDetails;
using CSETWeb_Api.BusinessManagers;
using CSETWeb_Api.Data.ControlData;
using CSETWeb_Api.Helpers;
using CSETWeb_Api.Models;
using DataLayerCore.Model;
using Nelibur.ObjectMapper;

namespace CSETWeb_Api.Controllers
{
    public class QuestionsController : ApiController
    {
        /// <summary>
        /// Returns a list of all applicable Questions or Requirements for the assessment.
        /// 
        /// A shorter list can be retrieved for a single Question_Group_Heading 
        /// by sending in the 'group' argument.  I'm not sure we need this yet. 
        /// </summary>
        [HttpPost]
        [Route("api/QuestionList")]
        public QuestionResponse GetList([FromBody]string group)
        {
            int assessmentId = Auth.AssessmentForUser();
            string applicationMode = GetApplicationMode(assessmentId);


            if (applicationMode.ToLower().StartsWith("questions"))
            {
                QuestionsManager qm = new QuestionsManager(assessmentId);
                QuestionResponse resp = qm.GetQuestionList(group);
                return resp;
            }
            else
            {
                RequirementsManager rm = new RequirementsManager(assessmentId);
                QuestionResponse resp = rm.GetRequirementsList();
                return resp;
            }
        }


        /// <summary>
        /// Determines if the assessment is question or requirements based.
        /// </summary>
        /// <param name="assessmentId"></param>
        /// <returns></returns>
        protected string GetApplicationMode(int assessmentId)
        {
            using (var db = new CSET_Context())
            {
                var mode = db.STANDARD_SELECTION.Where(x => x.Assessment_Id == assessmentId).Select(x => x.Application_Mode).FirstOrDefault();

                if (mode == null)
                {
                    // default to Questions mode
                    mode = "Q";
                    SetMode(mode);
                }

                return mode;
            }
        }


        /// <summary>
        /// Sets the application mode to be question or requirements based.
        /// </summary>
        /// <param name="mode"></param>
        [HttpPost]
        [Route("api/SetMode")]
        public void SetMode([FromUri]string mode)
        {
            int assessmentId = Auth.AssessmentForUser();
            QuestionsManager qm = new QuestionsManager(assessmentId);
            qm.SetApplicationMode(mode);
        }

        /// <summary>
        /// Gets the application mode (question or requirements based).
        /// </summary>
        [HttpGet]
        [Route("api/GetMode")]
        public string GetMode()
        {
            int assessmentId = Auth.AssessmentForUser();
            QuestionsManager qm = new QuestionsManager(assessmentId);
            string mode = GetApplicationMode(assessmentId).Trim().Substring(0, 1);
            if (String.IsNullOrEmpty(mode))
            {
                SetMode("Q");
                return "Q";
            } else
            {
                return mode;
            }
        }


        /// <summary>
        /// Persists an answer.  This includes Y/N/NA/A as well as comments and alt text.
        /// </summary>
        [HttpPost]
        [Route("api/AnswerQuestion")]
        public int StoreAnswer([FromBody]Answer answer)
        {
            if (answer == null)
                return 0;

            int assessmentId = Auth.AssessmentForUser();
            string applicationMode = GetApplicationMode(assessmentId);

            if (applicationMode.ToLower().StartsWith("questions"))
            {
                QuestionsManager qm = new QuestionsManager(assessmentId);
                return qm.StoreAnswer(answer);
            }
            else
            {
                RequirementsManager rm = new RequirementsManager(assessmentId);
                return rm.StoreAnswer(answer);
            }
        }


        /// <summary>
        /// Returns the details under a given questions details
        /// </summary>
        /// <param name="QuestionId"></param>
        [HttpPost]
        [Route("api/Details")]
        public QuestionDetailsContentViewModel GetDetails([FromUri] int QuestionId)
        {
            int assessmentId = Auth.AssessmentForUser();
            string applicationMode = GetApplicationMode(assessmentId);

            QuestionsManager qm = new QuestionsManager(assessmentId);
            return qm.GetDetails(QuestionId, assessmentId);

        }


        /// <summary>
        /// Persists a single answer to the SUB_CATEGORY_ANSWERS table for the 'block answer',
        /// and flips all of the constituent questions' answers.
        /// </summary>
        /// <param name="subCatAnswers"></param>
        [HttpPost]
        [Route("api/AnswerSubcategory")]
        public void StoreSubcategoryAnswers([FromBody]SubCategoryAnswers subCatAnswers)
        {
            int assessmentId = Auth.AssessmentForUser();

            QuestionsManager qm = new QuestionsManager(assessmentId);
            qm.StoreSubcategoryAnswers(subCatAnswers);
        }


        /// <summary>
        /// Note that this only populates the summary/title and finding id. 
        /// the rest is populated in a seperate call. 
        /// </summary>
        /// <param name="Answer_Id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/AnswerAllDiscoveries")]
        public List<Finding> AllDiscoveries([FromUri] int Answer_Id)
        {
            int assessmentId = Auth.AssessmentForUser();
            using (CSET_Context context = new CSET_Context())
            {
                FindingsViewModel fm = new FindingsViewModel(context, assessmentId, Answer_Id);
                return fm.AllFindings();
            }
        }

        [HttpPost]
        [Route("api/GetFinding")]
        public Finding GetFinding([FromUri] int Answer_Id, int Finding_id, int Question_Id)
        {
            int assessmentId = Auth.AssessmentForUser();
            using (CSET_Context context = new CSET_Context())
            {
                if (Answer_Id == 0)
                {
                    QuestionsManager questions = new QuestionsManager(assessmentId);
                    Answer_Id = questions.StoreAnswer(new Answer()
                    {
                        QuestionId = Question_Id,
                        MarkForReview = false
                    });
                }
                FindingsViewModel fm = new FindingsViewModel(context, assessmentId, Answer_Id);
                return fm.GetFinding(Finding_id);
            }
        }

        [HttpGet]
        [Route("api/GetImportance")]
        public List<Importance> GetImportance()
        {
            int assessmentId = Auth.AssessmentForUser();
            List<Importance> rlist = new List<Importance>();
            using (CSET_Context context = new CSET_Context())
            {
                foreach (IMPORTANCE import in context.IMPORTANCE)
                {
                    rlist.Add(TinyMapper.Map<Importance>(import));
                }
            }
            return rlist;
        }

        [HttpPost]
        [Route("api/DeleteFinding")]
        public void DeleteFinding([FromBody] int Finding_Id)
        {
            int assessmentId = Auth.AssessmentForUser();
            using (CSET_Context context = new CSET_Context())
            {
                FindingViewModel fm = new FindingViewModel(Finding_Id, context);
                fm.Delete();
            }
        }

        [HttpPost]
        [Route("api/AnswerSaveDiscovery")]
        public void SaveDiscovery([FromBody] Finding finding)
        {
            int assessmentId = Auth.AssessmentForUser();
            using (CSET_Context context = new CSET_Context())
            {
                FindingViewModel fm = new FindingViewModel(finding, context);
                fm.Save();
            }
        }


        /// <summary>
        /// Changes the title of a stored document.
        /// </summary>
        /// <param name="id">The document ID</param>
        /// <param name="title">The new title</param>
        [HttpPost]
        [Route("api/RenameDocument")]
        public void RenameDocument([FromUri] int id, string title)
        {
            int assessmentId = Auth.AssessmentForUser();
            new DocumentManager(assessmentId).RenameDocument(id, title);
        }


        /// <summary>
        /// Detaches a stored document from the answer.  
        /// </summary>
        /// <param name="id">The document ID</param>
        /// <param name="answerId">The document ID</param>
        [HttpPost]
        [Route("api/DeleteDocument")]
        public void DeleteDocument([FromUri] int id, int answerId)
        {
            int assessmentId = Auth.AssessmentForUser();
            new DocumentManager(assessmentId).DeleteDocument(id, answerId);
        }


        /// <summary>
        /// Returns a collection of all 
        /// </summary>
        /// <param name="id">The document ID</param>
        [HttpGet]
        [Route("api/QuestionsForDocument")]
        public List<int> GetQuestionsForDocument([FromUri] int id)
        {
            int assessmentId = Auth.AssessmentForUser();
            return new DocumentManager(assessmentId).GetQuestionsForDocument(id);
        }


        /// <summary>
        /// Returns a collection of Parameters with assessment-level overrides
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/ParametersForAssessment")]
        public List<ParameterToken> GetDefaultParametersForAssessment()
        {
            int assessmentId = Auth.AssessmentForUser();
            RequirementsManager rm = new RequirementsManager(assessmentId);

            return rm.GetDefaultParametersForAssessment();
        }


        /// <summary>
        /// Persists an assessment-level ("global") Parameter change.
        /// </summary>
        [HttpPost]
        [Route("api/SaveAssessmentParameter")]
        public ParameterToken SaveAssessmentParameter([FromBody] ParameterToken token)
        {
            int assessmentId = Auth.AssessmentForUser();
            RequirementsManager rm = new RequirementsManager(assessmentId);

            return rm.SaveAssessmentParameter(token.Id, token.Substitution);
        }


        /// <summary>
        /// Persists an answer-level ("in-line", "local") Parameter change.
        /// </summary>
        [HttpPost]
        [Route("api/SaveAnswerParameter")]
        public ParameterToken SaveAnswerParameter([FromBody] ParameterToken token)
        {
            int assessmentId = Auth.AssessmentForUser();
            RequirementsManager rm = new RequirementsManager(assessmentId);

            return rm.SaveAnswerParameter(token.RequirementId, token.Id, token.AnswerId, token.Substitution);
        }
    }
}

